using Modular.Common;

namespace Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Decrease;
internal sealed record DecreaseProductQuantityRequest(int ProductId, uint Quantity) : IDomainEvent;
