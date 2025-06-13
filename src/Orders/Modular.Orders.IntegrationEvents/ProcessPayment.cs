using Modular.Common;

namespace Modular.Orders.Integrations;
public sealed record ProcessPayment(Guid OrderId, Guid CustomerId, Price TotalAmount) : IIntegrationEvent;
