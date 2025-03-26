using Modular.Common;

namespace Modular.Orders.IntegrationEvents;
public sealed record ProcessPayment(Guid OrderId, Guid CustomerId, Price TotalAmount) : IIntegrationEvent;
