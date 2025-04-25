using Modular.Common;

namespace Modular.Warehouse.IntegrationEvents;

public sealed class ProductQuantityIncreasedInWarehouseIntegrationEvent : IIntegrationEvent
{
    public string Sku { get; init; }
    public uint Quantity { get; init; }

    public ProductQuantityIncreasedInWarehouseIntegrationEvent() { }

    public ProductQuantityIncreasedInWarehouseIntegrationEvent(string sku, uint quantity)
        => (Sku, Quantity) = (sku, quantity);
}
