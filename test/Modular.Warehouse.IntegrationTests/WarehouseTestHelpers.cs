using Marten;
using Modular.Warehouse.SourceModels;

namespace Modular.Warehouse.IntegrationTests;

internal static class WarehouseTestHelpers
{
    public static string NewSku() => Guid.NewGuid().ToString("N")[..12];

    public static async Task SeedProductAsync(IDocumentStore store, string sku, string name = "Test Product", uint initialQuantity = 0)
    {
        await using IDocumentSession session = store.LightweightSession();

        ProductCreated productCreated = new(sku, name, DateTimeOffset.UtcNow);
        session.Events.StartStream<Modular.Warehouse.UseCases.Products.Models.Product>(sku, productCreated);

        if (initialQuantity > 0)
        {
            ProductReceived productReceived = new(sku, initialQuantity, DateTimeOffset.UtcNow);
            session.Events.Append(sku, productReceived);
        }

        await session.SaveChangesAsync();
    }

    public static async Task DelistProductAsync(IDocumentStore store, string sku)
    {
        await using IDocumentSession session = store.LightweightSession();
        session.Events.Append(sku, new ProductDelisted(sku, DateTimeOffset.UtcNow));
        await session.SaveChangesAsync();
    }
}
