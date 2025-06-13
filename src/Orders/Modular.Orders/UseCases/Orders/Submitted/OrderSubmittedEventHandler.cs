using MassTransit;
using Modular.Orders.Integrations;

namespace Modular.Orders.UseCases.Orders.Submitted;
internal sealed class OrderSubmittedEventHandler : IConsumer<OrderSubmittedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderSubmittedEventHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<OrderSubmittedEvent> context)
    {
        ProcessPayment processPayment = new(context.Message.OrderId, context.Message.CustomerId, context.Message.TotalAmount);
        await _publishEndpoint.Publish(processPayment);

    }
}
