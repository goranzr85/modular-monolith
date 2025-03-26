using Modular.Common;

namespace Modular.Orders.Change.ChangeProductQuantity;
internal sealed record IncreasedProductQuantity(Guid OrderId, int ProductId, uint Quantity) : IDomainEvent;
