using ErrorOr;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Warehouse.UseCases.Products.Models;
using Modular.Warehouse.UseCases.Products.Receiving;
using Xunit;

namespace Modular.Warehouse.IntegrationTests;

[Collection(nameof(WarehouseDatabaseCollection))]
public sealed class ProductReceivingCommandTests
{
    private readonly WarehouseDatabaseFixture _fixture;

    public ProductReceivingCommandTests(WarehouseDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_WithExistingProduct_IncreasesQuantity()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        IDocumentStore store = app.Services.GetRequiredService<IDocumentStore>();
        ProductReceivingCommandHandler handler = app.Services.GetRequiredService<ProductReceivingCommandHandler>();

        string sku = WarehouseTestHelpers.NewSku();
        await WarehouseTestHelpers.SeedProductAsync(store, sku, initialQuantity: 5);

        ErrorOr<Unit> result = await handler.Handle(new ProductReceivingCommand(sku, 10), CancellationToken.None);

        Assert.False(result.IsError);

        await using IDocumentSession session = store.LightweightSession();
        Product? product = await session.LoadAsync<Product>(sku);
        Assert.NotNull(product);
        Assert.Equal(15u, product.Quantity);
    }

    [Fact]
    public async Task Handle_WithUnknownSku_ReturnsProductNotFound()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        ProductReceivingCommandHandler handler = app.Services.GetRequiredService<ProductReceivingCommandHandler>();

        ErrorOr<Unit> result = await handler.Handle(new ProductReceivingCommand(WarehouseTestHelpers.NewSku(), 10), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Product.NotFound", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithDelistedProduct_ReturnsProductDelisted()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        IDocumentStore store = app.Services.GetRequiredService<IDocumentStore>();
        ProductReceivingCommandHandler handler = app.Services.GetRequiredService<ProductReceivingCommandHandler>();

        string sku = WarehouseTestHelpers.NewSku();
        await WarehouseTestHelpers.SeedProductAsync(store, sku);
        await WarehouseTestHelpers.DelistProductAsync(store, sku);

        ErrorOr<Unit> result = await handler.Handle(new ProductReceivingCommand(sku, 10), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Product.Delisted", result.FirstError.Code);
    }

    [Theory]
    [InlineData("", 10u)]
    [InlineData("valid-sku", 0u)]
    public async Task Handle_WithInvalidInput_ReturnsValidationError(string sku, uint quantity)
    {
        // Regression test: AddWarehouse previously registered validators without includeInternalTypes,
        // so ProductReceivingCommandValidator (internal) was never actually registered and validation
        // silently never ran.
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        ProductReceivingCommandHandler handler = app.Services.GetRequiredService<ProductReceivingCommandHandler>();

        ErrorOr<Unit> result = await handler.Handle(new ProductReceivingCommand(sku, quantity), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }
}
