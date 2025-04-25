using Modular.Common;
using Modular.Orders.UseCases.Orders.Models;

namespace Modular.Orders.UseCases.Orders.Create;
internal sealed record OrderCreatedEvent(Guid OrderId, List<OrderItem> OrderItems) : IDomainEvent;
