using Modular.Common.Events;

namespace Modular.Customers.IntegrationEvents;

public sealed record CustomerCreatedEvent(Guid Id, FullName FullName, Address Address, ContactInfo Contact) : IIntegrationEvent, IDomainEvent;
