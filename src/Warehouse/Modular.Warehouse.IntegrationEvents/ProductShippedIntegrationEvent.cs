using Modular.Common;

namespace Modular.Warehouse.IntegrationEvents;

public sealed class ProductShippedIntegrationEvent : IIntegrationEvent
{
    public string Sku { get; init; }
    public uint Quantity { get; init; }

    public ProductShippedIntegrationEvent() { }

    public ProductShippedIntegrationEvent(string sku, uint quantity)
        => (Sku, Quantity) = (sku, quantity);
}
