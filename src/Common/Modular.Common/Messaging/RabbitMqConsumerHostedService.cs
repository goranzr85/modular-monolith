using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modular.Common.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Modular.Common.Messaging;

// Generic consumer host: reflects over every closed IIntegrationEventConsumer<T> that TConsumer implements,
// binds one durable queue (named after TConsumer) to the fanout exchange for each T, and dispatches incoming
// messages by their AMQP "type" property. Replaces MassTransit's per-consumer receive endpoint + retry.
public sealed class RabbitMqConsumerHostedService<TConsumer> : BackgroundService
    where TConsumer : class
{
    private const int MaxDeliveryAttempts = 5;

    private readonly IConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumerHostedService<TConsumer>> _logger;
    private readonly string _queueName;
    private readonly IReadOnlyDictionary<string, MessageHandler> _handlersByTypeName;
    private IChannel? _channel;

    public RabbitMqConsumerHostedService(
        IConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqConsumerHostedService<TConsumer>> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueName = RabbitMqIntegrationEventNaming.QueueFor(typeof(TConsumer));
        _handlersByTypeName = BuildHandlers();

        if (_handlersByTypeName.Count == 0)
        {
            throw new InvalidOperationException(
                $"{typeof(TConsumer).Name} does not implement IIntegrationEventConsumer<T> for any message type.");
        }
    }

    // Topology setup and BasicConsumeAsync happen here (awaited to completion) rather than in ExecuteAsync:
    // BackgroundService.StartAsync does not wait for ExecuteAsync to reach its first await, so a publish
    // issued right after StartAsync() returns could otherwise race the consumer's own queue binding.
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        string deadLetterExchange = $"{_queueName}.dlx";
        string deadLetterQueue = $"{_queueName}.dlq";

        await _channel.ExchangeDeclareAsync(deadLetterExchange, ExchangeType.Fanout, durable: true,
            cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(deadLetterQueue, deadLetterExchange, routingKey: string.Empty,
            cancellationToken: cancellationToken);

        Dictionary<string, object?> queueArguments = new() { ["x-dead-letter-exchange"] = deadLetterExchange };
        await _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false,
            arguments: queueArguments, cancellationToken: cancellationToken);

        foreach (MessageHandler handler in _handlersByTypeName.Values)
        {
            string exchange = RabbitMqIntegrationEventNaming.ExchangeFor(handler.MessageType);
            await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, durable: true,
                cancellationToken: cancellationToken);
            await _channel.QueueBindAsync(_queueName, exchange, routingKey: string.Empty,
                cancellationToken: cancellationToken);
        }

        await _channel.BasicQosAsync(0, prefetchCount: 10, global: false, cancellationToken: cancellationToken);

        // CancellationToken.None: in-flight message handling must run to completion even during shutdown -
        // tying it to the host's stopping token would cancel a handler mid-retry, poisoning every remaining
        // attempt with an already-cancelled token and wrongly dead-lettering a message that was never broken,
        // just interrupted by a normal deploy/restart.
        AsyncEventingBasicConsumer consumer = new(_channel);
        consumer.ReceivedAsync += (_, ea) => HandleMessageAsync(ea, CancellationToken.None);

        await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer, cancellationToken: cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Delay(Timeout.Infinite, stoppingToken);

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        string? typeName = ea.BasicProperties.Type;

        if (typeName is null || !_handlersByTypeName.TryGetValue(typeName, out MessageHandler? handler))
        {
            _logger.LogWarning("Queue {Queue} received an unrecognized message type {Type}; dead-lettering.",
                _queueName, typeName);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false,
                cancellationToken: stoppingToken);
            return;
        }

        object? message;
        try
        {
            message = JsonSerializer.Deserialize(ea.Body.Span, handler.MessageType, RabbitMqJsonOptions.Default);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Queue {Queue} failed to deserialize message of type {Type}; dead-lettering.",
                _queueName, typeName);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false,
                cancellationToken: stoppingToken);
            return;
        }

        if (message is null)
        {
            _logger.LogError("Queue {Queue} deserialized a null message of type {Type}; dead-lettering.",
                _queueName, typeName);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false,
                cancellationToken: stoppingToken);
            return;
        }

        for (int attempt = 1; attempt <= MaxDeliveryAttempts; attempt++)
        {
            try
            {
                await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                TConsumer consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();
                await handler.Invoke(consumer, message, stoppingToken);

                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                return;
            }
            catch (Exception ex) when (attempt < MaxDeliveryAttempts)
            {
                _logger.LogWarning(ex,
                    "Queue {Queue} attempt {Attempt}/{MaxAttempts} failed for message type {Type}; retrying.",
                    _queueName, attempt, MaxDeliveryAttempts, typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Queue {Queue} giving up on message type {Type} after {MaxAttempts} attempts; dead-lettering.",
                    _queueName, typeName, MaxDeliveryAttempts);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false,
                    cancellationToken: stoppingToken);
                return;
            }
        }
    }

    private static IReadOnlyDictionary<string, MessageHandler> BuildHandlers()
    {
        Dictionary<string, MessageHandler> handlers = new();

        foreach (Type consumerInterface in typeof(TConsumer).GetInterfaces())
        {
            if (!consumerInterface.IsGenericType
                || consumerInterface.GetGenericTypeDefinition() != typeof(IIntegrationEventConsumer<>))
            {
                continue;
            }

            Type messageType = consumerInterface.GetGenericArguments()[0];
            MethodInfo consumeMethod = consumerInterface.GetMethod(nameof(IIntegrationEventConsumer<object>.ConsumeAsync))!;

            Task Invoke(TConsumer consumer, object message, CancellationToken ct) =>
                (Task)consumeMethod.Invoke(consumer, [message, ct])!;

            handlers[messageType.FullName!] = new MessageHandler(messageType, Invoke);
        }

        return handlers;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }

    private sealed record MessageHandler(Type MessageType, Func<TConsumer, object, CancellationToken, Task> Invoke);
}
