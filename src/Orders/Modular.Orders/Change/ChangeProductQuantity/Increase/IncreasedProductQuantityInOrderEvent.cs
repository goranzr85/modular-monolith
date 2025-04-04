using Modular.Common;

namespace Modular.Orders.Change.ChangeProductQuantity.Increase;
internal sealed record IncreasedProductQuantityInOrderEvent(Guid OrderId, int ProductId, uint Quantity) : IDomainEvent;
