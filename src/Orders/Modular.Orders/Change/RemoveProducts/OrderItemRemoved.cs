using Modular.Common;

namespace Modular.Orders.Change.RemoveProducts;
internal sealed record OrderItemRemoved(int ProductId, uint Quantity) : IDomainEvent;
