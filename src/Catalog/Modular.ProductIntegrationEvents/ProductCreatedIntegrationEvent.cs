using Modular.Common;

namespace Modular.Catalog.IntegrationEvents;

public sealed record ProductCreatedIntegrationEvent(string Sku, string Name, string Description, Price Price) : IIntegrationEvent;

