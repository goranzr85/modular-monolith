using Modular.Common.Events;

namespace Modular.Customers.IntegrationEvents;
public sealed record CustomerChangedShippingAddressEvent(Guid CustomerId, Address ShippingAddress) : IIntegrationEvent, IDomainEvent;
