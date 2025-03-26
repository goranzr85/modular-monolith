using Modular.Common;

namespace Modular.Orders.Change;
internal sealed record DecreasedProductQuantity(Guid OrderId, int ProductId, uint Quantity) : IDomainEvent;
