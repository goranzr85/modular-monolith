using Modular.Common;

namespace Modular.Warehouse.IntegrationEvents;

public sealed class ProductShippedIntegrationEvent : IIntegrationEvent
{
    public string Sku { get; init; }
    public uint Quantity { get; init; }
    public Guid OrderId { get; init; }
    public DateTimeOffset ShippedDate{ get; init; }

    public ProductShippedIntegrationEvent() { }

    public ProductShippedIntegrationEvent(string sku, uint quantity, Guid orderId, DateTimeOffset shippedDate)
        => (Sku, Quantity, OrderId, ShippedDate) = (sku, quantity, orderId, shippedDate);
}
