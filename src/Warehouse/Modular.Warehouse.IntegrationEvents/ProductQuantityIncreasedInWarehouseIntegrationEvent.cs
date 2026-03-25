using Modular.Common.Events;

namespace Modular.Warehouse.IntegrationEvents;

public sealed class ProductQuantityIncreasedInWarehouseIntegrationEvent : IIntegrationEvent
{
    public string Sku { get; init; }
    public uint Quantity { get; init; }
    public DateTimeOffset OccuredOnUtc { get; init; }

    public ProductQuantityIncreasedInWarehouseIntegrationEvent() { }

    public ProductQuantityIncreasedInWarehouseIntegrationEvent(string sku, uint quantity, DateTimeOffset occuredOnUtc)
        => (Sku, Quantity, OccuredOnUtc) = (sku, quantity, occuredOnUtc);
}
