using ErrorOr;
using Marten;
using MediatR;
using Microsoft.Extensions.Logging;
using Modular.Warehouse.Errors;
using Modular.Warehouse.SourceModels;

namespace Modular.Warehouse.Shipping;

internal sealed record ProductShippingCommand(string Sku, uint Quantity) : IRequest<ErrorOr<Unit>>
{
}

internal sealed class ProductShippingCommandHandler : IRequestHandler<ProductShippingCommand, ErrorOr<Unit>>
{
    private readonly IDocumentStore _documentStore;
    private readonly TimeProvider _dateTimeProvider;
    private readonly ILogger<ProductShippingCommandHandler> _logger;

    public ProductShippingCommandHandler(IDocumentStore documentStore, ILogger<ProductShippingCommandHandler> logger, TimeProvider dateTimeProvider)
    {
        _documentStore = documentStore;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ErrorOr<Unit>> Handle(ProductShippingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shipping product {Sku} with quantity {Quantity}.", request.Sku, request.Quantity);

        await using var session = _documentStore.LightweightSession();
        Product? product = await session.Events.AggregateStreamAsync<Product>(request.Sku, token: cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("Product with SKU: {Sku} does not exist", request.Sku);
            return ProductErrors.ProductNotFound(request.Sku);
        }

        if (product.IsDelisted)
        {
            _logger.LogWarning("Product with SKU: {Sku} is delisted", request.Sku);
            return ProductErrors.ProductDelisted(request.Sku);
        }

        ProductShipped productShipped = new(request.Sku, request.Quantity, _dateTimeProvider.GetUtcNow());
        session.Events.Append(productShipped.Sku, productShipped);
        await session.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Shipping product {Sku} with quantity {Quantity} succeeded.", request.Sku, request.Quantity);
        return Unit.Value;
    }
}