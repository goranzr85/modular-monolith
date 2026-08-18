using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Catalog.UseCases.Create;
using Modular.Common;
using Xunit;

namespace Modular.Catalog.IntegrationTests;

[Collection(nameof(CatalogDatabaseCollection))]
public sealed class CreateProductCommandTests
{
    private readonly CatalogDatabaseFixture _fixture;

    public CreateProductCommandTests(CatalogDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static string NewSku() => Guid.NewGuid().ToString("N")[..12];

    [Fact]
    public async Task Handle_WithNewSku_PersistsProductAndOutboxMessage()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        CreateProductCommandHandler handler = scope.ServiceProvider.GetRequiredService<CreateProductCommandHandler>();
        CatalogDbContext dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        string sku = NewSku();
        CreateProductCommand command = new(sku, "Test Product", "A product used for testing.", 19.99m);

        ErrorOr<Unit> result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsError);

        Product? product = await dbContext.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == sku);

        Assert.NotNull(product);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal("A product used for testing.", product.Description);
        Assert.Equal(19.99m, (decimal)product.Price);

        OutboxMessage? outboxMessage = await dbContext.OutboxMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Type == "ProductCreatedEvent" && m.Content.Contains(sku));

        Assert.NotNull(outboxMessage);
        Assert.Null(outboxMessage.ProcessedOnUtc);
    }

    [Fact]
    public async Task Handle_WithDuplicateSku_ReturnsErrorAndDoesNotDuplicatePersistence()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        CreateProductCommandHandler handler = scope.ServiceProvider.GetRequiredService<CreateProductCommandHandler>();
        CatalogDbContext dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        string sku = NewSku();
        CreateProductCommand command = new(sku, "Test Product", "A product used for testing.", 19.99m);

        ErrorOr<Unit> firstResult = await handler.Handle(command, CancellationToken.None);
        Assert.False(firstResult.IsError);

        ErrorOr<Unit> secondResult = await handler.Handle(command, CancellationToken.None);

        Assert.True(secondResult.IsError);
        Assert.Equal(ErrorType.Failure, secondResult.FirstError.Type);
        Assert.Equal("Product.AlreadyExists", secondResult.FirstError.Code);

        int productCount = await dbContext.Products.AsNoTracking().CountAsync(p => p.Sku == sku);
        Assert.Equal(1, productCount);
    }

    [Theory]
    [InlineData("", "Name", "Description")]
    [InlineData("Sku", "", "Description")]
    [InlineData("Sku", "Name", "")]
    public async Task Handle_WithInvalidProductData_ReturnsValidationErrorAndPersistsNothing(
        string sku, string name, string description)
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        CreateProductCommandHandler handler = scope.ServiceProvider.GetRequiredService<CreateProductCommandHandler>();
        CatalogDbContext dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        string effectiveSku = string.IsNullOrEmpty(sku) ? sku : NewSku();
        CreateProductCommand command = new(effectiveSku, name, description, 19.99m);

        ErrorOr<Unit> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);

        if (!string.IsNullOrEmpty(effectiveSku))
        {
            bool exists = await dbContext.Products.AsNoTracking().AnyAsync(p => p.Sku == effectiveSku);
            Assert.False(exists);
        }
    }
}
