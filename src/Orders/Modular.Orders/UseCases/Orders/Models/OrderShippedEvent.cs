using Modular.Common;

namespace Modular.Orders.UseCases.Orders.Models;
internal sealed record OrderShippedEvent(Guid Orderid, Guid CustomerId, Price TotalAmount) : IDomainEvent;
