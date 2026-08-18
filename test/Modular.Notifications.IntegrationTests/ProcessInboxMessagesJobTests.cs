using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Notifications.Infrastructure.BackgroundJobs;
using Modular.Notifications.Infrastructure.NotificationSenders;
using Modular.Orders.Integrations;
using Xunit;

namespace Modular.Notifications.IntegrationTests;

[Collection(nameof(NotificationDatabaseCollection))]
public sealed class ProcessInboxMessagesJobTests
{
    private readonly NotificationDatabaseFixture _fixture;

    public ProcessInboxMessagesJobTests(NotificationDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UnkeyedNotificationSender_ResolvesToRoutingFactory()
    {
        // Regression test: ProcessInboxMessagesJob previously requested
        // [FromKeyedServices(NotificationSenderFactory.Key)], and that key string ("SmsNotificationsSender")
        // collided with SmsNotificationsSender's own keyed registration, so the job resolved the raw SMS
        // sender directly instead of NotificationSenderFactory's Email/Phone routing logic.
        await using NotificationTestApp app = await _fixture.CreateAppAsync();
        using IServiceScope scope = app.Services.CreateScope();

        INotificationSender sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        Assert.IsType<NotificationSenderFactory>(sender);
    }

    [Fact]
    public async Task Execute_WithPendingOrderShippedMessage_SendsNotificationAndMarksProcessed()
    {
        // Regression test: OrderShippedNotificationHandler previously serialized the payload with
        // System.Text.Json while this job deserialized with Newtonsoft.Json + TypeNameHandling.All -
        // the payload never carried $type metadata, so deserialization produced a JObject instead of an
        // OrderShippedIntegrationEvent and every message was skipped as "invalid domain event type".
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
        await app.Harness.Bus.Publish(shippedEvent);
        Assert.True(await app.Harness.Consumed.Any<OrderShippedIntegrationEvent>());

        ProcessInboxMessagesJob job = ActivatorUtilities.CreateInstance<ProcessInboxMessagesJob>(scope.ServiceProvider);
        await job.Execute(null!);

        InboxMessage? inboxMessage = await dbContext.InboxMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.MessageType == nameof(OrderShippedIntegrationEvent) && m.Payload.Contains(orderId.ToString()));

        Assert.NotNull(inboxMessage);
        Assert.NotNull(inboxMessage.ProcessedAt);
    }

    [Fact]
    public async Task Execute_WithNoPendingMessages_CompletesWithoutError()
    {
        await using NotificationTestApp app = await _fixture.CreateAppAsync();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

        ProcessInboxMessagesJob job = ActivatorUtilities.CreateInstance<ProcessInboxMessagesJob>(scope.ServiceProvider);

        await job.Execute(null!);
    }
}
