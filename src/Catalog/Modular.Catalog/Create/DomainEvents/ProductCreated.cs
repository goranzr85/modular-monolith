using Modular.Common;

namespace Modular.Catalog.Create.DomainEvents;

public sealed record ProductCreated(string Sku, string Name, string Description, Price Price) : IDomainEvent;
