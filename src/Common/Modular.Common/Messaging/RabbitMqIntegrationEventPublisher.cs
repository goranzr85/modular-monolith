using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Modular.Common.Events;
using RabbitMQ.Client;

namespace Modular.Common.Messaging;

public sealed class RabbitMqIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqIntegrationEventPublisher> _logger;

    public RabbitMqIntegrationEventPublisher(IConnection connection, ILogger<RabbitMqIntegrationEventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task PublishAsync(object message, string? causationTraceParent = null, CancellationToken cancellationToken = default)
    {
        Type messageType = message.GetType();
        string exchange = RabbitMqIntegrationEventNaming.ExchangeFor(messageType);

        using Activity? activity = RabbitMqTelemetry.ActivitySource.StartActivity(
            $"{exchange} publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", exchange);
        activity?.SetTag("messaging.destination_kind", "fanout");
        activity?.SetTag("messaging.message.type", messageType.FullName);

        if (activity is not null && causationTraceParent is not null
            && ActivityContext.TryParse(causationTraceParent, traceState: null, isRemote: true, out ActivityContext causationContext))
        {
            activity.AddLink(new ActivityLink(causationContext));
        }

        await using IChannel channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, messageType, RabbitMqJsonOptions.Default);

        BasicProperties properties = new()
        {
            Persistent = true,
            Type = messageType.FullName
        };

        if (activity is not null)
        {
            properties.Headers ??= new Dictionary<string, object?>();
            DistributedContextPropagator.Current.Inject(activity, properties, static (carrier, key, value) =>
            {
                ((BasicProperties)carrier!).Headers![key] = value;
            });
        }

        try
        {
            await channel.BasicPublishAsync(exchange, routingKey: string.Empty, mandatory: false,
                basicProperties: properties, body: body, cancellationToken: cancellationToken);

            RabbitMqTelemetry.MessagesPublished.Add(1, new KeyValuePair<string, object?>("messaging.destination", exchange));
            _logger.LogDebug("Published {MessageType} to exchange {Exchange}.", messageType.FullName, exchange);
        }
        catch (Exception ex)
        {
            activity.RecordException(ex);
            _logger.LogError(ex, "Failed to publish {MessageType} to exchange {Exchange}.", messageType.FullName, exchange);
            throw;
        }
    }
}
