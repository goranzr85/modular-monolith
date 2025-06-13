using ErrorOr;
using MediatR;
using Modular.Orders.UseCases.Orders.Models;

namespace Modular.Orders.Errors;

internal static class OrderErrors
{
    internal static Error OrderNotFound(Guid orderId) =>
        Error.NotFound("Order.NotFound", $"Order with ID '{orderId}' was not found.");
     
    internal static Error OrderStatusIllegalTransition(Guid orderId, OrderStatus oldOrderStatus, OrderStatus newOrderStatus) =>
        Error.Validation("Order.OrderStatusIllegalTransition", $"Order with ID '{orderId}' can not be changed from order status {oldOrderStatus} to {newOrderStatus}.");
    
    internal static Error OrderAlreadyCreated(Guid orderId) =>
         Error.Validation("Order.OrderAlreadyCreated", $"Order with ID '{orderId}' is already created.");

    internal static Error ProductNotFound(int productId) =>
         Error.NotFound("Order.ProductNotFound", $"Product with ID '{productId}' does not exist.");

    internal static ErrorOr<Unit> ProductIsNotPlaced(Guid orderId, int productId) =>
         Error.NotFound("Order.ProductIsNotPlaced", $"Product with ID '{productId}' is not placed in order '{orderId}'.");
    internal static ErrorOr<Unit> ProductIsNotPlaced(Guid orderId, string productSku) =>
         Error.NotFound("Order.ProductIsNotPlaced", $"Product with SKU '{productSku}' is not placed in order '{orderId}'.");

    internal static ErrorOr<Unit> AddItemToOrderError(Guid orderId, int productId) =>
        Error.Failure("Order.AddItemError", $"Product with ID '{productId}' is not placed in order '{orderId}'.");

    internal static ErrorOr<Unit> ProductQuantityIsNotEnough(int productId) =>
         Error.Validation("Order.NotEnoughProductQuantity", $"Not enough product quantity with ID '{productId}'.");

    internal static ErrorOr<Unit> ProductQuantityIsNotEnoughForDecrease(Guid orderId, int productId, uint quantity)=>
        Error.Validation("Order.ProductQuantityIsNotEnoughForDecrease", $" Can not be removed '{quantity}' pieces of product with ID '{productId}' from order with ID '{orderId}'. There is no enough pieces of product placed in order.");
    internal static ErrorOr<Unit> ProductAlreadyShipped(Guid orderId, string productSku) =>
    Error.Validation("Order.ProductAlreadyShipped", $" Product with SKU '{productSku}' from order with ID '{orderId}' is already shipped.");

    internal static ErrorOr<Unit> IncreaseProductQuantityError(Guid orderId, int productId, uint quantity) =>
        Error.Failure("Order.IncreaseProductQuantityError", $"An error occurred while increasing quantity ('{quantity}' pieces) for product with ID '{productId}' in order '{orderId}'.");
   
    internal static ErrorOr<Unit> DecreaseProductQuantityError(Guid orderId, int productId, uint quantity) =>
        Error.Failure("Order.DecreaseProductQuantityError", $"An error occurred while decreasing quantity ('{quantity}' pieces) for product with ID '{productId}' in order '{orderId}'.");

}

