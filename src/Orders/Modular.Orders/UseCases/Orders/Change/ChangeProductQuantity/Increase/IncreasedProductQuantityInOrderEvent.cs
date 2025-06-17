using Modular.Common.Events;

namespace Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Increase;
internal sealed record IncreasedProductQuantityInOrderEvent(Guid OrderId, int ProductId, uint Quantity) : IDomainEvent;
