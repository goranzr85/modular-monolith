namespace Modular.Catalog.Change;

internal sealed record ChangeProductRequest(string Sku, string Name, string Description, decimal Price)
{
}