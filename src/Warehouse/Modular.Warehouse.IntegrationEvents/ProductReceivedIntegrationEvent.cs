using Modular.Common;

namespace Modular.Warehouse.IntegrationEvents;

public sealed class ProductReceivedIntegrationEvent : IIntegrationEvent
{
    public string Sku { get; init; }
    public uint Quantity { get; init; }

    public ProductReceivedIntegrationEvent() { }

    public ProductReceivedIntegrationEvent(string sku, uint quantity)
        => (Sku, Quantity) = (sku, quantity);
}
