using MassTransit;
using MassTransit.Initializers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Orders.Integrations;
using Modular.Orders.UseCases.Orders.Models;
using Modular.Payments.IntegrationEvents;
using Modular.Warehouse.IntegrationEvents;

namespace Modular.Orders.UseCases.Orders.Submitted;
internal sealed class OrderSubmittedOrchestration : IConsumer<OrderSubmittedEvent>,
    IConsumer<PaymentProcessedIntegrationEvent>,
    IConsumer<ProductShippedIntegrationEvent>,
    IConsumer<OrderShippedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<OrderSubmittedOrchestration> _logger;

    public OrderSubmittedOrchestration(IPublishEndpoint publishEndpoint, OrderDbContext orderDbContext, ILogger<OrderSubmittedOrchestration> logger)
    {
        _publishEndpoint = publishEndpoint;
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderSubmittedEvent> context)
    {
        ProcessPayment processPayment = new(context.Message.OrderId, context.Message.CustomerId, context.Message.TotalAmount);
        await _publishEndpoint.Publish(processPayment);
    }

    public async Task Consume(ConsumeContext<PaymentProcessedIntegrationEvent> context)
    {
        List<OrderItem> orderItems = await _orderDbContext.Orders
            .SingleOrDefaultAsync(o => o.Id == context.Message.OrderId)
            .Select(o => o!.Items);

        ShipProduct shipProduct = new(context.Message.OrderId, orderItems.Select(oi => (oi.Product.SKU, oi.Quantity)).ToArray());

        await _publishEndpoint.Publish(shipProduct);
    }

    public async Task Consume(ConsumeContext<ProductShippedIntegrationEvent> context)
    {
        Order? order = await _orderDbContext.Orders
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == context.Message.OrderId);

        if (order is null)
        {
            _logger.LogError("Order {OrderId} not found for shipping confirmation.", context.Message.OrderId);
            return;
        }

        order.MarkItemAsShipped(context.Message.Sku);
    }

    public async Task Consume(ConsumeContext<OrderShippedEvent> context)
    {
        OrderShippedIntegrationEvent orderShippedIntegrationEvent = await _orderDbContext.Orders
            .FirstAsync(o => o.Id == context.Message.Orderid)
            .Select(o => new OrderShippedIntegrationEvent
            {
                OrderId = o.Id,
                CustomerId = o.CustomerId,
                ShippedDate = DateOnly.FromDateTime(o.ShippedDate!.Value.Date),
                Products = o.Items.Select(i => (i.Product.Name, i.Quantity, i.Price)).ToArray(),
            });

        await _publishEndpoint.Publish(orderShippedIntegrationEvent);
    }
}
