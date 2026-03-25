using Modular.Common.Events;

namespace Modular.Warehouse.IntegrationEvents;

public sealed class ProductShippedIntegrationEvent : IIntegrationEvent
{
    public string Sku { get; init; }
    public uint Quantity { get; init; }
    public Guid OrderId { get; init; }
    public DateTimeOffset ShippedDate { get; init; }
    public DateTimeOffset OccuredOnUtc { get; init; }

    public ProductShippedIntegrationEvent() { }

    public ProductShippedIntegrationEvent(string sku, uint quantity, Guid orderId, DateTimeOffset shippedDate, DateTimeOffset occuredOnUtc)
        => (Sku, Quantity, OrderId, ShippedDate, OccuredOnUtc) = (sku, quantity, orderId, shippedDate, occuredOnUtc);
}
