using Modular.Common;

namespace Modular.Catalog.UseCases.Create.DomainEvents;

public sealed record ProductCreatedEvent(string Sku, string Name, string Description, Price Price) : IDomainEvent;
