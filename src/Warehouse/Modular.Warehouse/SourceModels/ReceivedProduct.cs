namespace Modular.Warehouse.SourceModels;
internal sealed record ReceivedProduct(string Sku, uint Quantity, DateTime OccuredOnUtc)
{
}

internal sealed record ShippedProduct(string Sku, uint Quantity, DateTime OccuredOnUtc)
{
}

internal sealed record IncreasedProductQuantity(string Sku, uint Quantity, string Reason, DateTime OccuredOnUtc)
{
}

internal sealed record DecreasedProductQuantity(string Sku, uint Quantity, string Reason, DateTime OccuredOnUtc)
{
}

