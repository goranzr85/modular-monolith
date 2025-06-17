using Modular.Common.Events;

namespace Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Increase;
internal sealed record IncreaseProductQuantityRequest(int ProductId, uint Quantity) : IDomainEvent;
