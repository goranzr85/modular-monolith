namespace Modular.Catalog.Change;

internal sealed record ChangeProductRequest(Guid Id, string Sku, string Name, string Description, decimal Price)
{
}