using Modular.Common;

namespace Modular.Orders.Change.ChangeProductQuantity.Increase;
internal sealed record IncreaseProductQuantityRequest(int ProductId, uint Quantity) : IDomainEvent;
