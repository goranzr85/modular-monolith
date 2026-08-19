using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Orders.Integrations;
using Xunit;

namespace Modular.Notifications.IntegrationTests;

[Collection(nameof(NotificationDatabaseCollection))]
public sealed class OrderShippedNotificationHandlerTests
{
    private readonly NotificationDatabaseFixture _fixture;

    public OrderShippedNotificationHandlerTests(NotificationDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Consume_OrderShippedIntegrationEvent_PersistsUnprocessedInboxMessage()
    {
        // Regression test: InboxMessage.ProcessedAt is DateTimeOffset? but was previously mapped
        // .IsRequired() (NOT NULL) while this handler never sets it on insert, so every insert violated
        // the NOT NULL constraint.
        await using NotificationTestApp app = await _fixture.CreateAppAsync();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        NotificationDbContext dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        Guid orderId = Guid.NewGuid();
        OrderShippedIntegrationEvent shippedEvent = new()
        {
            OrderId = orderId,
            CustomerId = Guid.NewGuid(),
            ShippedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Products = [("Widget", 2u, Price.Create(9.99m))],
            TotalAmounts = Price.Create(19.98m),
        };

        await app.Publisher.PublishAsync(shippedEvent);

        InboxMessage? inboxMessage = await Eventually.WaitForAsync(() => dbContext.InboxMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MessageType == nameof(OrderShippedIntegrationEvent) && m.Payload.Contains(orderId.ToString())));

        Assert.NotNull(inboxMessage);
        Assert.Null(inboxMessage.ProcessedAt);
    }
}
