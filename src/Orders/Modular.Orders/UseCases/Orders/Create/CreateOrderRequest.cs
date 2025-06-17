using Modular.Orders.UseCases.Orders.Models;

namespace Modular.Orders.UseCases.Orders.Create;
internal sealed record CreateOrderRequest(Guid CustomerId, List<OrderItem> Items);
