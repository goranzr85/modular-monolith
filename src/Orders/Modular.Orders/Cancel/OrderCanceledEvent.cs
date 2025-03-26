using Modular.Common;

namespace Modular.Orders.Cancel;
internal sealed record OrderCanceledEvent(Guid OrderId) : IDomainEvent;