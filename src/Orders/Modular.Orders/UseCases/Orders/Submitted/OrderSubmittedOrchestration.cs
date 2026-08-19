using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Common.Events;
using Modular.Orders.Integrations;
using Modular.Orders.UseCases.Orders.Models;
using Modular.Payments.IntegrationEvents;
using Modular.Warehouse.IntegrationEvents;

namespace Modular.Orders.UseCases.Orders.Submitted;
internal sealed class OrderSubmittedOrchestration : IIntegrationEventConsumer<OrderSubmittedEvent>,
    IIntegrationEventConsumer<PaymentProcessedIntegrationEvent>,
    IIntegrationEventConsumer<ProductShippedIntegrationEvent>,
    IIntegrationEventConsumer<OrderShippedEvent>
{
    private readonly IIntegrationEventPublisher _publisher;
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<OrderSubmittedOrchestration> _logger;

    public OrderSubmittedOrchestration(IIntegrationEventPublisher publisher, OrderDbContext orderDbContext, ILogger<OrderSubmittedOrchestration> logger)
    {
        _publisher = publisher;
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task ConsumeAsync(OrderSubmittedEvent message, CancellationToken cancellationToken)
    {
        ProcessPayment processPayment = new(message.OrderId, message.CustomerId, message.TotalAmount);
        await _publisher.PublishAsync(processPayment, cancellationToken);
    }

    public async Task ConsumeAsync(PaymentProcessedIntegrationEvent message, CancellationToken cancellationToken)
    {
        Order? order = await _orderDbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Product)
            .SingleOrDefaultAsync(o => o.Id == message.OrderId, cancellationToken);

        if (order is null)
        {
            _logger.LogError("Order {OrderId} not found for payment confirmation.", message.OrderId);
            return;
        }

        ShipProduct shipProduct = new(message.OrderId, order.Items.Select(oi => (oi.Product.SKU, oi.Quantity)).ToArray());

        await _publisher.PublishAsync(shipProduct, cancellationToken);
    }

    public async Task ConsumeAsync(ProductShippedIntegrationEvent message, CancellationToken cancellationToken)
    {
        Order? order = await _orderDbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Product)
            .SingleOrDefaultAsync(o => o.Id == message.OrderId, cancellationToken);

        if (order is null)
        {
            _logger.LogError("Order {OrderId} not found for shipping confirmation.", message.OrderId);
            return;
        }

        order.MarkItemAsShipped(message.Sku);

        await _orderDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ConsumeAsync(OrderShippedEvent message, CancellationToken cancellationToken)
    {
        Order order = await _orderDbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Product)
            .FirstAsync(o => o.Id == message.Orderid, cancellationToken);

        OrderShippedIntegrationEvent orderShippedIntegrationEvent = new()
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            ShippedDate = DateOnly.FromDateTime(order.ShippedDate!.Value.Date),
            Products = order.Items.Select(i => (i.Product.Name, i.Quantity, i.Price)).ToArray(),
        };

        await _publisher.PublishAsync(orderShippedIntegrationEvent, cancellationToken);
    }
}
