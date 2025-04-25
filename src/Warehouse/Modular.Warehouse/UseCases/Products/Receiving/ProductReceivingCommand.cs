using ErrorOr;
using Marten;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Modular.Warehouse.Errors;
using Modular.Warehouse.IntegrationEvents;
using Modular.Warehouse.Models;
using Modular.Warehouse.SourceModels;

namespace Modular.Warehouse.UseCases.Products.Receiving;

internal sealed record ProductReceivingCommand(string Sku, uint Quantity) : IRequest<ErrorOr<Unit>>;

internal sealed class ProductReceivingCommandHandler : IRequestHandler<ProductReceivingCommand, ErrorOr<Unit>>
{
    private readonly IDocumentStore _documentStore;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ProductReceivingCommandHandler> _logger;

    public ProductReceivingCommandHandler(IDocumentStore documentStore, ILogger<ProductReceivingCommandHandler> logger, IPublishEndpoint publishEndpoint)
    {
        _documentStore = documentStore;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ErrorOr<Unit>> Handle(ProductReceivingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receiving product {Sku} with quantity {Quantity}.", request.Sku, request.Quantity);

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

        ProductReceived productReceived = new(request.Sku, request.Quantity, DateTime.UtcNow);
        session.Events.Append(productReceived.Sku, productReceived);

        await _publishEndpoint.Publish(new ProductQuantityIncreasedInWarehouseIntegrationEvent(request.Sku, request.Quantity), cancellationToken);

        await session.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Receiving product {Sku} with quantity {Quantity} succeeded.", request.Sku, request.Quantity);
        return Unit.Value;
    }
}