using MassTransit;
using Modular.Orders.IntegrationEvents;

namespace Modular.Orders.UseCases.Orders.Submitted;
internal sealed class OrderSubmittedEventHandler : IConsumer<OrderSubmittedEvent>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderSubmittedEventHandler(OrderDbContext orderDbContext, IPublishEndpoint publishEndpoint)
    {
        _orderDbContext = orderDbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<OrderSubmittedEvent> context)
    {
        ProcessPayment processPayment = new(context.Message.OrderId, context.Message.CustomerId, context.Message.TotalAmount);
        await _publishEndpoint.Publish(processPayment);

    }
}
