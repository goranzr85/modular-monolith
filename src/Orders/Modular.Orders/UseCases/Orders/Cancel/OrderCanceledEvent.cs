using Modular.Common.Events;

namespace Modular.Orders.UseCases.Orders.Cancel;
internal sealed record OrderCanceledEvent(Guid OrderId) : IDomainEvent;