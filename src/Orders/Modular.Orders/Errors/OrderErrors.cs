using ErrorOr;
using Modular.Common;
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

    internal static Error InvalidOrderId() =>
         Error.Validation("Order.InvalidOrderId", "OrderId cannot be empty.");

    internal static Error InvalidOrderDate() =>
         Error.Validation("Order.InvalidOrderDate", "OrderDate cannot be empty.");

    internal static Error InvalidCustomerId() =>
         Error.Validation("Order.InvalidCustomerId", "CustomerId cannot be empty.");

    internal static Error EmptyItems() =>
         Error.Validation("Order.EmptyItems", "Items cannot be null or empty.");

    internal static Error InvalidProductSku() =>
         Error.Validation("Order.InvalidProductSku", "SKU cannot be empty.");

    internal static Error InvalidProductName() =>
         Error.Validation("Order.InvalidProductName", "Name cannot be empty.");

    internal static Error InvalidProductDescription() =>
         Error.Validation("Order.InvalidProductDescription", "Description cannot be empty.");

    internal static Error InvalidProductPrice() =>
         Error.Validation("Order.InvalidProductPrice", "Price cannot be less than or equal to zero.");

    internal static Error InsufficientStock(int productId) =>
         Error.Validation("Order.InsufficientStock", $"Insufficient stock quantity for product with ID '{productId}'.");

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

