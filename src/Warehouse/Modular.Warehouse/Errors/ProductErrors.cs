using ErrorOr;

namespace Modular.Warehouse.Errors;

internal static class ProductErrors
{
    public static Error ProductNotFound(Guid id) =>
        Error.NotFound("Product.NotFound", $"Product with Id '{id}' was not found.");

    public static Error NotEnoughQuantity() =>
        Error.Validation("Product.NotEnoughQuantity", "Not enough quantity");


}
