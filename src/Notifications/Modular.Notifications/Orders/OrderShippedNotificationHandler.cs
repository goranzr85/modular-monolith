using Modular.Common.Events;
using Modular.Orders.Integrations;
using Newtonsoft.Json;

namespace Modular.Notifications.Orders;
internal sealed class OrderShippedNotificationHandler : IIntegrationEventConsumer<OrderShippedIntegrationEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly TimeProvider _dateTimeProvider;

    public OrderShippedNotificationHandler(NotificationDbContext dbContext, TimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task ConsumeAsync(OrderShippedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var inboxMessage = new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = nameof(OrderShippedIntegrationEvent),
            Payload = JsonConvert.SerializeObject(message, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            }),
            ReceivedAt = _dateTimeProvider.GetUtcNow(),
        };

        await _dbContext.InboxMessages.AddAsync(inboxMessage, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
