using Modular.Common;

namespace Modular.Orders.Change.ChangeProductQuantity.Decrease;
internal sealed record DecreasedProductQuantityInOrderEvent(Guid OrderId, int ProductId, uint Quantity) : IDomainEvent;
