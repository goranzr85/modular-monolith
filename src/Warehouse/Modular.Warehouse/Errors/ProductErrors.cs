using ErrorOr;

namespace Modular.Warehouse.Errors;

internal static class ProductErrors
{
    public static Error ProductNotFound(string sku) =>
        Error.NotFound("Product.NotFound", $"Product with SKU '{sku}' was not found.");
    
    public static Error ProductAlreadyExist(string sku) =>
        Error.Conflict("Product.AlreadyExists", $"Product with SKU '{sku}' already exists.");

      public static Error ProductDelisted(string sku) =>
        Error.NotFound("Product.Delisted", $"Product with SKU '{sku}' was delisted.");

    public static Error NotEnoughQuantity() =>
        Error.Conflict("Product.NotEnoughQuantity", "Not enough quantity");

    public static Error ConcurrentModification(string sku) =>
        Error.Conflict("Product.ConcurrentModification", $"Product with SKU '{sku}' was modified concurrently. Please retry.");

}
