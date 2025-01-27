namespace Modular.Catalog.Create;

internal sealed record CreateProductRequest(string Sku, string Name, string Description, decimal Price)
{
}