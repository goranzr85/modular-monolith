using ErrorOr;

namespace Modular.Catalog.Errors;

public static class ProductErrors
{
    public static Error ProductNotFound(string sku) =>
        Error.NotFound("Product.NotFound", $"Product with SKU '{sku}' was not found.");

    public static Error ProductAlreadyExists(string sku) =>
        Error.Failure("Product.AlreadyExists", $"Product with SKU '{sku}' already exists.");

    public static Error ProductNotCreated(string sku) => Error.Failure("Product.Failure", $"Creating product with SKU '{sku}' failed.");

    public static Error InvalidSku() => Error.Validation("Product.InvalidSku", "SKU is not valid.");

    public static Error InvalidName() => Error.Validation("Product.InvalidName", "Name is not valid.");

    public static Error InvalidDescription() => Error.Validation("Product.InvalidDescription", "Description is not valid.");

    public static Error InvalidPrice() => Error.Validation("Product.InvalidPrice", "Price is not valid.");
}
