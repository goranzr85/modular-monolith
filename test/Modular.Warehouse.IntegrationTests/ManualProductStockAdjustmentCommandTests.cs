using ErrorOr;
using MediatR;
using Marten;
using Microsoft.Extensions.DependencyInjection;
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
        ISender sender = app.Services.GetRequiredService<ISender>();

        string sku = WarehouseTestHelpers.NewSku();
        await WarehouseTestHelpers.SeedProductAsync(store, sku, initialQuantity: 5);

        ErrorOr<Unit> result = await sender.Send(new ManualProductStockIncreaseCommand(sku, 3, "Stock count correction"));

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
        ISender sender = app.Services.GetRequiredService<ISender>();

        ErrorOr<Unit> result = await sender.Send(new ManualProductStockIncreaseCommand(WarehouseTestHelpers.NewSku(), 3, "Reason"));

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
        ISender sender = app.Services.GetRequiredService<ISender>();

        ErrorOr<Unit> result = await sender.Send(new ManualProductStockIncreaseCommand(sku, quantity, reason));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Decrease_WithSufficientQuantity_DecreasesQuantity()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        IDocumentStore store = app.Services.GetRequiredService<IDocumentStore>();
        ISender sender = app.Services.GetRequiredService<ISender>();

        string sku = WarehouseTestHelpers.NewSku();
        await WarehouseTestHelpers.SeedProductAsync(store, sku, initialQuantity: 10);

        ErrorOr<Unit> result = await sender.Send(new ManualProductStockDecreaseCommand(sku, 4, "Damaged goods"));

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
        ISender sender = app.Services.GetRequiredService<ISender>();

        string sku = WarehouseTestHelpers.NewSku();
        await WarehouseTestHelpers.SeedProductAsync(store, sku, initialQuantity: 2);

        ErrorOr<Unit> result = await sender.Send(new ManualProductStockDecreaseCommand(sku, 5, "Damaged goods"));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        Assert.Equal("Product.NotEnoughQuantity", result.FirstError.Code);
    }

    [Fact]
    public async Task Decrease_WithUnknownSku_ReturnsProductNotFound()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        ISender sender = app.Services.GetRequiredService<ISender>();

        ErrorOr<Unit> result = await sender.Send(new ManualProductStockDecreaseCommand(WarehouseTestHelpers.NewSku(), 1, "Reason"));

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
        ISender sender = app.Services.GetRequiredService<ISender>();

        ErrorOr<Unit> result = await sender.Send(new ManualProductStockDecreaseCommand(sku, quantity, reason));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }
}
