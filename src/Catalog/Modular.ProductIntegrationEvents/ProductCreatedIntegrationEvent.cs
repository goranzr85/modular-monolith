using Modular.Common;
using Modular.Common.Events;

namespace Modular.Catalog.IntegrationEvents;

public sealed record ProductCreatedIntegrationEvent : IIntegrationEvent
{
    public string Sku { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public Price Price { get; init; }

    public ProductCreatedIntegrationEvent() { }

    public ProductCreatedIntegrationEvent(string sku, string name, string description, Price price)
        => (Sku, Name, Description, Price) = (sku, name, description, price);
}
