using MassTransit;
using Modular.Orders.Integrations;

namespace Modular.Notifications;
internal sealed class OrderShippedNotificationHandler : IConsumer<OrderShippedIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderShippedIntegrationEvent> context)
    {
        return Task.CompletedTask;
    }
}
