using ErrorOr;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Warehouse.UseCases.Products.Adjusted.Decreased;
using Modular.Warehouse.UseCases.Products.Adjusted.Increased;
using Modular.Warehouse.UseCases.Products.Models;
using Xunit;

namespace Modular.Warehouse.IntegrationTests;

[Collection(nameof(WarehouseDatabaseCollection))]
public sealed class ManualProductStockAdjustmentCommandTests
{
    private readonly WarehouseDatabaseFixture _fixture;

    public ManualProductStockAdjustmentCommandTests(WarehouseDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Increase_WithExistingProduct_IncreasesQuantity()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        IDocumentStore store = app.Services.GetRequiredService<IDocumentStore>();
        ManualProductStockIncreaseCommandHandler handler = app.Services.GetRequiredService<ManualProductStockIncreaseCommandHandler>();

        string sku = WarehouseTestHelpers.NewSku();
        await WarehouseTestHelpers.SeedProductAsync(store, sku, initialQuantity: 5);

        ErrorOr<Unit> result = await handler.Handle(new ManualProductStockIncreaseCommand(sku, 3, "Stock count correction"), CancellationToken.None);

        Assert.False(result.IsError);

        await using IDocumentSession session = store.LightweightSession();
        Product? product = await session.LoadAsync<Product>(sku);
        Assert.NotNull(product);
        Assert.Equal(8u, product.Quantity);
    }

    [Fact]
    public async Task Increase_WithUnknownSku_ReturnsProductNotFound()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        ManualProductStockIncreaseCommandHandler handler = app.Services.GetRequiredService<ManualProductStockIncreaseCommandHandler>();

        ErrorOr<Unit> result = await handler.Handle(new ManualProductStockIncreaseCommand(WarehouseTestHelpers.NewSku(), 3, "Reason"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("Product.NotFound", result.FirstError.Code);
    }

    [Theory]
    [InlineData("", 1u, "Reason")]
    [InlineData("valid-sku", 0u, "Reason")]
    [InlineData("valid-sku", 1u, "")]
    public async Task Increase_WithInvalidInput_ReturnsValidationError(string sku, uint quantity, string reason)
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        ManualProductStockIncreaseCommandHandler handler = app.Services.GetRequiredService<ManualProductStockIncreaseCommandHandler>();

        ErrorOr<Unit> result = await handler.Handle(new ManualProductStockIncreaseCommand(sku, quantity, reason), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Decrease_WithSufficientQuantity_DecreasesQuantity()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        IDocumentStore store = app.Services.GetRequiredService<IDocumentStore>();
        ManualProductStockDecreaseCommandHandler handler = app.Services.GetRequiredService<ManualProductStockDecreaseCommandHandler>();

        string sku = WarehouseTestHelpers.NewSku();
        await WarehouseTestHelpers.SeedProductAsync(store, sku, initialQuantity: 10);

        ErrorOr<Unit> result = await handler.Handle(new ManualProductStockDecreaseCommand(sku, 4, "Damaged goods"), CancellationToken.None);

        Assert.False(result.IsError);

        await using IDocumentSession session = store.LightweightSession();
        Product? product = await session.LoadAsync<Product>(sku);
        Assert.NotNull(product);
        Assert.Equal(6u, product.Quantity);
    }

    [Fact]
    public async Task Decrease_WithInsufficientQuantity_ReturnsNotEnoughQuantity()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        IDocumentStore store = app.Services.GetRequiredService<IDocumentStore>();
        ManualProductStockDecreaseCommandHandler handler = app.Services.GetRequiredService<ManualProductStockDecreaseCommandHandler>();

        string sku = WarehouseTestHelpers.NewSku();
        await WarehouseTestHelpers.SeedProductAsync(store, sku, initialQuantity: 2);

        ErrorOr<Unit> result = await handler.Handle(new ManualProductStockDecreaseCommand(sku, 5, "Damaged goods"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        Assert.Equal("Product.NotEnoughQuantity", result.FirstError.Code);
    }

    [Fact]
    public async Task Decrease_WithUnknownSku_ReturnsProductNotFound()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        ManualProductStockDecreaseCommandHandler handler = app.Services.GetRequiredService<ManualProductStockDecreaseCommandHandler>();

        ErrorOr<Unit> result = await handler.Handle(new ManualProductStockDecreaseCommand(WarehouseTestHelpers.NewSku(), 1, "Reason"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("Product.NotFound", result.FirstError.Code);
    }

    [Theory]
    [InlineData("", 1u, "Reason")]
    [InlineData("valid-sku", 0u, "Reason")]
    [InlineData("valid-sku", 1u, "")]
    public async Task Decrease_WithInvalidInput_ReturnsValidationError(string sku, uint quantity, string reason)
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        ManualProductStockDecreaseCommandHandler handler = app.Services.GetRequiredService<ManualProductStockDecreaseCommandHandler>();

        ErrorOr<Unit> result = await handler.Handle(new ManualProductStockDecreaseCommand(sku, quantity, reason), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }
}
