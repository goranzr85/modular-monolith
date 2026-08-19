using System.Text.Json;
using Modular.Common.Events;
using RabbitMQ.Client;

namespace Modular.Common.Messaging;

public sealed class RabbitMqIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IConnection _connection;

    public RabbitMqIntegrationEventPublisher(IConnection connection)
    {
        _connection = connection;
    }

    public async Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        Type messageType = message.GetType();
        string exchange = RabbitMqIntegrationEventNaming.ExchangeFor(messageType);

        await using IChannel channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, messageType, RabbitMqJsonOptions.Default);

        BasicProperties properties = new()
        {
            Persistent = true,
            Type = messageType.FullName
        };

        await channel.BasicPublishAsync(exchange, routingKey: string.Empty, mandatory: false,
            basicProperties: properties, body: body, cancellationToken: cancellationToken);
    }
}
