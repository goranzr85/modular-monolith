using Modular.Common;
using Modular.Common.Events;

namespace Modular.Orders.UseCases.Orders.Submitted;
public sealed record OrderSubmittedEvent(Guid OrderId, Guid CustomerId, Price TotalAmount) : IDomainEvent;
