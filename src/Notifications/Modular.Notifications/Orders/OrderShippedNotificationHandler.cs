using MassTransit;
using Modular.Orders.Integrations;

namespace Modular.Notifications.Orders;
internal sealed class OrderShippedNotificationHandler : IConsumer<OrderShippedIntegrationEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly TimeProvider _dateTimeProvider;

    public OrderShippedNotificationHandler(NotificationDbContext dbContext, TimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Consume(ConsumeContext<OrderShippedIntegrationEvent> context)
    {
        var inboxMessage = new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = nameof(OrderShippedIntegrationEvent),
            Payload = System.Text.Json.JsonSerializer.Serialize(context.Message),
            ReceivedAt = _dateTimeProvider.GetUtcNow(),
        };

        await _dbContext.InboxMessages.AddAsync(inboxMessage);
        await _dbContext.SaveChangesAsync();
    }
}
