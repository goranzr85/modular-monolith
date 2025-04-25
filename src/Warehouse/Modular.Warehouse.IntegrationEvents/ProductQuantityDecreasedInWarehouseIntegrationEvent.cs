using Modular.Common;

namespace Modular.Warehouse.IntegrationEvents;

public sealed class ProductQuantityDecreasedInWarehouseIntegrationEvent : IIntegrationEvent
{
    public string Sku { get; init; }
    public uint Quantity { get; init; }

    public ProductQuantityDecreasedInWarehouseIntegrationEvent() { }

    public ProductQuantityDecreasedInWarehouseIntegrationEvent(string sku, uint quantity)
        => (Sku, Quantity) = (sku, quantity);
}
