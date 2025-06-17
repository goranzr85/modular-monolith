using Modular.Common;
using Modular.Common.Events;

namespace Modular.Orders.Integrations;
public sealed record ProcessPayment(Guid OrderId, Guid CustomerId, Price TotalAmount) : IIntegrationEvent;
