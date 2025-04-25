namespace Modular.Catalog.UseCases.Change;

internal sealed record ChangeProductRequest(string Sku, string Name, string Description, decimal Price)
{
}