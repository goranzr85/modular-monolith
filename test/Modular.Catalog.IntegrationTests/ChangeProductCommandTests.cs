using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Catalog.UseCases.Change;
using Modular.Catalog.UseCases.Create;
using Modular.Common;
using Xunit;

namespace Modular.Catalog.IntegrationTests;

[Collection(nameof(CatalogDatabaseCollection))]
public sealed class ChangeProductCommandTests
{
    private readonly CatalogDatabaseFixture _fixture;

    public ChangeProductCommandTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static string NewSku() => Guid.NewGuid().ToString("N")[..12];

    private static async Task<string> SeedProductAsync(ISender sender)
    {
        string sku = NewSku();
        ErrorOr<Unit> result = await sender.Send(new CreateProductCommand(sku, "Original Name", "Original description.", 9.99m));
        Assert.False(result.IsError);

        return sku;
    }

    [Fact]
    public async Task Handle_WithExistingProduct_PersistsChanges()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        CatalogDbContext dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        string sku = await SeedProductAsync(sender);

        ChangeProductCommand command = new(sku, "Updated Name", "Updated description.", 29.99m);
        ErrorOr<Unit> result = await sender.Send(command);

        Assert.False(result.IsError);

        Product? product = await dbContext.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == sku);

        Assert.NotNull(product);
        Assert.Equal("Updated Name", product.Name);
        Assert.Equal("Updated description.", product.Description);
        Assert.Equal(29.99m, (decimal)product.Price);
    }

    [Fact]
    public async Task Handle_WithUnknownSku_ReturnsNotFound()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        ChangeProductCommand command = new(NewSku(), "Name", "Description", 9.99m);

        ErrorOr<Unit> result = await sender.Send(command);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Product.NotFound", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidData_ReturnsValidationErrorAndDoesNotChangeProduct()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        CatalogDbContext dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        string sku = await SeedProductAsync(sender);

        ChangeProductCommand command = new(sku, "Updated Name", "Updated description.", -5m);
        ErrorOr<Unit> result = await sender.Send(command);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);

        Product? product = await dbContext.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == sku);

        Assert.NotNull(product);
        Assert.Equal("Original Name", product.Name);
    }
}
