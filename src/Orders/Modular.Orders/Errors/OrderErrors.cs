using ErrorOr;
using MediatR;

namespace Modular.Orders.Errors;

internal static class OrderErrors
{
    internal static Error OrderNotFound(Guid orderId) =>
        Error.NotFound("Order.NotFound", $"Order with ID '{orderId}' was not found.");
    internal static Error OrderAlreadyCreated(Guid orderId) =>
         Error.NotFound("Order.OrderAlreadyCreated", $"Order with ID '{orderId}' is already created.");

    internal static Error ProductNotFound(int productId) =>
         Error.NotFound("Order.ProductNotFound", $"Product with ID '{productId}' does not exist.");

    internal static ErrorOr<Unit> ProductIsNotPlaced(Guid orderId, int productId) =>
         Error.NotFound("Order.ProductIsNotPlaced", $"Product with ID '{productId}' is not placed in order '{orderId}'.");

    internal static ErrorOr<Unit> ProductQuantityIsNotEnough(Guid id, int productId, uint quantity)
    {
        throw new NotImplementedException();
    }
}

