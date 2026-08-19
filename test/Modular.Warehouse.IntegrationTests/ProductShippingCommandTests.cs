using ErrorOr;
using MediatR;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Modular.Warehouse.UseCases.Products.Models;
using Modular.Warehouse.UseCases.Products.Shipping;
using Xunit;

namespace Modular.Warehouse.IntegrationTests;

[Collection(nameof(WarehouseDatabaseCollection))]
public sealed class ProductShippingCommandTests
{
    private readonly WarehouseDatabaseFixture _fixture;

    public ProductShippingCommandTests(WarehouseDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_WithSufficientQuantity_DecreasesQuantity()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        IDocumentStore store = app.Services.GetRequiredService<IDocumentStore>();
        ISender sender = app.Services.GetRequiredService<ISender>();

        string sku = WarehouseTestHelpers.NewSku();
        await WarehouseTestHelpers.SeedProductAsync(store, sku, initialQuantity: 10);

        ErrorOr<Unit> result = await sender.Send(new ProductShippingCommand(sku, 4, Guid.NewGuid()));

        Assert.False(result.IsError);

        await using IDocumentSession session = store.LightweightSession();
        Product? product = await session.LoadAsync<Product>(sku);
        Assert.NotNull(product);
        Assert.Equal(6u, product.Quantity);
    }

    [Fact]
    public async Task Handle_WithInsufficientQuantity_ReturnsNotEnoughQuantityAndDoesNotChangeStock()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        IDocumentStore store = app.Services.GetRequiredService<IDocumentStore>();
        ISender sender = app.Services.GetRequiredService<ISender>();

        string sku = WarehouseTestHelpers.NewSku();
        await WarehouseTestHelpers.SeedProductAsync(store, sku, initialQuantity: 2);

        ErrorOr<Unit> result = await sender.Send(new ProductShippingCommand(sku, 5, Guid.NewGuid()));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        Assert.Equal("Product.NotEnoughQuantity", result.FirstError.Code);

        await using IDocumentSession session = store.LightweightSession();
        Product? product = await session.LoadAsync<Product>(sku);
        Assert.NotNull(product);
        Assert.Equal(2u, product.Quantity);
    }

    [Fact]
    public async Task Handle_WithUnknownSku_ReturnsProductNotFound()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        ISender sender = app.Services.GetRequiredService<ISender>();

        ErrorOr<Unit> result = await sender.Send(new ProductShippingCommand(WarehouseTestHelpers.NewSku(), 1, Guid.NewGuid()));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Product.NotFound", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithDelistedProduct_ReturnsProductDelisted()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        IDocumentStore store = app.Services.GetRequiredService<IDocumentStore>();
        ISender sender = app.Services.GetRequiredService<ISender>();

        string sku = WarehouseTestHelpers.NewSku();
        await WarehouseTestHelpers.SeedProductAsync(store, sku, initialQuantity: 10);
        await WarehouseTestHelpers.DelistProductAsync(store, sku);

        ErrorOr<Unit> result = await sender.Send(new ProductShippingCommand(sku, 1, Guid.NewGuid()));

        Assert.True(result.IsError);
        Assert.Equal("Product.Delisted", result.FirstError.Code);
    }

    [Theory]
    [InlineData("", 1u)]
    [InlineData("valid-sku", 0u)]
    public async Task Handle_WithInvalidInput_ReturnsValidationError(string sku, uint quantity)
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        ISender sender = app.Services.GetRequiredService<ISender>();

        ErrorOr<Unit> result = await sender.Send(new ProductShippingCommand(sku, quantity, Guid.NewGuid()));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_WithEmptyOrderId_ReturnsValidationError()
    {
        await using WarehouseTestApp app = await _fixture.CreateAppAsync();
        ISender sender = app.Services.GetRequiredService<ISender>();

        ErrorOr<Unit> result = await sender.Send(new ProductShippingCommand(WarehouseTestHelpers.NewSku(), 1, Guid.Empty));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }
}
