using Modular.Common.Events;

namespace Modular.Customers.IntegrationEvents;
public sealed record CustomerChangedContactInformationEvent(Guid CustomerId, ContactInfo ContactInfo) : IIntegrationEvent, IDomainEvent;
