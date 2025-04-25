using ErrorOr;
using Marten;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Modular.Warehouse.Errors;
using Modular.Warehouse.IntegrationEvents;
using Modular.Warehouse.Models;
using Modular.Warehouse.SourceModels;

namespace Modular.Warehouse.UseCases.Products.Adjusted.Decreased;

internal sealed record ManualProductStockDecreaseCommand(string Sku, uint Quantity, string Reason) : IRequest<ErrorOr<Unit>>;

internal sealed class ManualProductStockDecreaseCommandHandler : IRequestHandler<ManualProductStockDecreaseCommand, ErrorOr<Unit>>
{
    private readonly IDocumentStore _documentStore;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ManualProductStockDecreaseCommandHandler> _logger;

    public ManualProductStockDecreaseCommandHandler(IDocumentStore documentStore, ILogger<ManualProductStockDecreaseCommandHandler> logger, IPublishEndpoint publishEndpoint)
    {
        _documentStore = documentStore;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ErrorOr<Unit>> Handle(ManualProductStockDecreaseCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Decreasing product {Sku} for quantity {Quantity}. Reason: {Reason}.", request.Sku, request.Quantity, request.Reason);

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

        DecreasedProductQuantity productReceived = new(request.Sku, request.Quantity, request.Reason, DateTime.UtcNow);
        session.Events.Append(productReceived.Sku, productReceived);

        await _publishEndpoint.Publish(new ProductQuantityDecreasedInWarehouseIntegrationEvent(request.Sku, request.Quantity), cancellationToken);

        await session.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Decreasing product {Sku} for quantity {Quantity} succeeded.", request.Sku, request.Quantity);
        return Unit.Value;
    }
}