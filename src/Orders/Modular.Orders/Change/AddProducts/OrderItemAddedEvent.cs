using Modular.Common;

namespace Modular.Orders.Change.AddProducts;
internal sealed record OrderItemAddedEvent(int ProductId, uint Quantity) : IDomainEvent;