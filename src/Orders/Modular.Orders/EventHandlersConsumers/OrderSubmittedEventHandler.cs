using MassTransit;
using Modular.Orders.DomainEvents;
using Modular.Orders.IntegrationEvents;

namespace Modular.Orders.EventHandlersConsumers;
internal sealed class OrderSubmittedEventHandler : IConsumer<OrderSubmitted>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderSubmittedEventHandler(OrderDbContext orderDbContext, IPublishEndpoint publishEndpoint)
    {
        _orderDbContext = orderDbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        ProcessPayment processPayment = new(context.Message.OrderId, context.Message.CustomerId, context.Message.TotalAmount);
        await _publishEndpoint.Publish(processPayment);

    }
}
