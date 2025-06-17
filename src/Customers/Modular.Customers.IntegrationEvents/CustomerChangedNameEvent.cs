using Modular.Common.Events;

namespace Modular.Customers.IntegrationEvents;
public sealed record CustomerChangedNameEvent(Guid CustomerId, FullName FullName) : IIntegrationEvent, IDomainEvent;
