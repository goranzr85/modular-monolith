using Modular.Common;
using Modular.Orders.Models;

namespace Modular.Orders.Create;
internal sealed record OrderCreated(Guid OrderId, List<OrderItem> OrderItems) : IDomainEvent;
