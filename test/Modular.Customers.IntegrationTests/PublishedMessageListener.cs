using System.Text.Json;
using Modular.Common.Messaging;
using RabbitMQ.Client;

namespace Modular.Customers.IntegrationTests;

// Test-only stand-in for MassTransit's ITestHarness.Published: binds a private queue to a message type's
// exchange BEFORE the code under test runs, then polls that queue for a message matching the predicate.
public sealed class PublishedMessageListener<TMessage> : IAsyncDisposable
{
    private readonly IChannel _channel;
    private readonly string _queueName;

    private PublishedMessageListener(IChannel channel, string queueName)
    {
        _channel = channel;
        _queueName = queueName;
    }

    public static async Task<PublishedMessageListener<TMessage>> StartAsync(IConnection connection)
    {
        IChannel channel = await connection.CreateChannelAsync();
        string exchange = RabbitMqIntegrationEventNaming.ExchangeFor(typeof(TMessage));

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, durable: true);
        QueueDeclareOk queue = await channel.QueueDeclareAsync(queue: string.Empty, durable: false, exclusive: true, autoDelete: true);
        await channel.QueueBindAsync(queue.QueueName, exchange, routingKey: string.Empty);

        return new PublishedMessageListener<TMessage>(channel, queue.QueueName);
    }

    public async Task<bool> AnyAsync(Func<TMessage, bool> predicate, TimeSpan? timeout = null)
    {
        DateTime deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));

        while (DateTime.UtcNow < deadline)
        {
            BasicGetResult? result = await _channel.BasicGetAsync(_queueName, autoAck: true);

            if (result is not null)
            {
                TMessage? message = JsonSerializer.Deserialize<TMessage>(result.Body.Span, RabbitMqJsonOptions.Default);

                if (message is not null && predicate(message))
                {
                    return true;
                }
            }
            else
            {
                await Task.Delay(100);
            }
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
    }
}
