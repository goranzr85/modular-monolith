using Modular.Common;

namespace Modular.Orders.UseCases.Orders.Submitted;
public sealed record OrderSubmittedEvent(Guid OrderId, Guid CustomerId, Price TotalAmount) : IDomainEvent;
