using ErrorOr;

namespace Modular.Warehouse.Errors;

internal static class ProductErrors
{
    public static Error ProductNotFound(string sku) =>
        Error.NotFound("Product.NotFound", $"Product with SKU '{sku}' was not found.");

    public static Error NotEnoughQuantity() =>
        Error.Validation("Product.NotEnoughQuantity", "Not enough quantity");


}
