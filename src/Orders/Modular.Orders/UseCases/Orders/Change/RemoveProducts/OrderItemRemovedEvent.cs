using Modular.Common.Events;

namespace Modular.Orders.UseCases.Orders.Change.RemoveProducts;
internal sealed record OrderItemRemovedEvent(int ProductId, uint Quantity) : IDomainEvent;
