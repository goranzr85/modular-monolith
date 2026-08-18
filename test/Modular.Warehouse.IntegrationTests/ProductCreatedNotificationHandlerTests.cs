using MassTransit;
using MassTransit.Testing;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Modular.Catalog.IntegrationEvents;
using Modular.Common;
using Modular.Warehouse.UseCases.Products.Models;
using Xunit;

namespace Modular.Warehouse.IntegrationTests;

[Collection(nameof(WarehouseDatabaseCollection))]
public sealed class ProductCreatedNotificationHandlerTests
{
    private readonly WarehouseDatabaseFixture _fixture;

    public ProductCreatedNotificationHandlerTests(WarehouseDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static string NewSku() => Guid.NewGuid().ToString("N")[..12];

    [Fact]
    public async Task Consume_WithNewSku_StartsProductStreamAtZeroQuantity()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        IDocumentStore store = app.Services.GetRequiredService<IDocumentStore>();

        string sku = NewSku();
        ProductCreatedIntegrationEvent createdEvent = new(sku, "Test Product", "A product used for testing.", Price.Create(9.99m));

        await app.Harness.Bus.Publish(createdEvent);
        Assert.True(await app.Harness.Consumed.Any<ProductCreatedIntegrationEvent>());
        Assert.False(await app.Harness.Published.Any<Fault<ProductCreatedIntegrationEvent>>());

        await using IDocumentSession session = store.LightweightSession();
        Product? product = await session.LoadAsync<Product>(sku);

        Assert.NotNull(product);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(0u, product.Quantity);
        Assert.False(product.IsDelisted);
    }

    [Fact]
    public async Task Consume_WithAlreadyExistingSku_DoesNotThrowOrDuplicate()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        IDocumentStore store = app.Services.GetRequiredService<IDocumentStore>();

        string sku = NewSku();
        ProductCreatedIntegrationEvent createdEvent = new(sku, "Test Product", "A product used for testing.", Price.Create(9.99m));

        await app.Harness.Bus.Publish(createdEvent);
        Assert.True(await app.Harness.Consumed.Any<ProductCreatedIntegrationEvent>());

        await app.Harness.Bus.Publish(createdEvent);
        Assert.True(await app.Harness.Consumed.Any<ProductCreatedIntegrationEvent>(x => x.Context.Message.Sku == sku));
        Assert.False(await app.Harness.Published.Any<Fault<ProductCreatedIntegrationEvent>>());

        await using IDocumentSession session = store.LightweightSession();
        Product? product = await session.LoadAsync<Product>(sku);
        Assert.NotNull(product);
        Assert.Equal(0u, product.Quantity);
    }
}
