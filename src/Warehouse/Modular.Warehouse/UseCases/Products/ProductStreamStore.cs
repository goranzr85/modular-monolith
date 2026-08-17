using ErrorOr;
using Marten;
using Marten.Events;
using Microsoft.Extensions.Logging;
using Modular.Warehouse.Errors;
using Modular.Warehouse.UseCases.Products.Models;

namespace Modular.Warehouse.UseCases.Products;

internal sealed record ProductWriteContext(Product Product, IEventStream<Product> Stream);

internal interface IProductStreamStore
{
    Task<ErrorOr<Product>> LoadAsync(IDocumentSession session, string sku, CancellationToken cancellationToken);

    Task<ErrorOr<ProductWriteContext>> LoadForWritingAsync(IDocumentSession session, string sku, CancellationToken cancellationToken);
}

internal sealed class ProductStreamStore : IProductStreamStore
{
    private readonly ILogger<ProductStreamStore> _logger;

    public ProductStreamStore(ILogger<ProductStreamStore> logger)
    {
        _logger = logger;
    }

    public async Task<ErrorOr<Product>> LoadAsync(IDocumentSession session, string sku, CancellationToken cancellationToken)
    {
        Product? product = await session.LoadAsync<Product>(sku, cancellationToken);

        return Validate(product, sku);
    }

    public async Task<ErrorOr<ProductWriteContext>> LoadForWritingAsync(IDocumentSession session, string sku, CancellationToken cancellationToken)
    {
        Product? product = await session.LoadAsync<Product>(sku, cancellationToken);

        ErrorOr<Product> validation = Validate(product, sku);
        if (validation.IsError)
        {
            return validation.Errors;
        }

        IEventStream<Product> stream = await session.Events.FetchForWriting<Product>(sku, cancellationToken);

        return new ProductWriteContext(validation.Value, stream);
    }

    private ErrorOr<Product> Validate(Product? product, string sku)
    {
        if (product is null)
        {
            _logger.LogWarning("Product with SKU: {Sku} does not exist", sku);
            return ProductErrors.ProductNotFound(sku);
        }

        if (product.IsDelisted)
        {
            _logger.LogWarning("Product with SKU: {Sku} is delisted", sku);
            return ProductErrors.ProductDelisted(sku);
        }

        return product;
    }
}
