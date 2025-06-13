using ErrorOr;
using Marten;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Modular.Warehouse.Errors;
using Modular.Warehouse.IntegrationEvents;
using Modular.Warehouse.Models;
using Modular.Warehouse.SourceModels;

namespace Modular.Warehouse.UseCases.Products.Shipping;

internal sealed record ProductShippingCommand(string Sku, uint Quantity, Guid OrderId) : IRequest<ErrorOr<Unit>>;

internal sealed class ProductShippingCommandHandler : IRequestHandler<ProductShippingCommand, ErrorOr<Unit>>
{
    private readonly IDocumentStore _documentStore;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly TimeProvider _dateTimeProvider;
    private readonly ILogger<ProductShippingCommandHandler> _logger;

    public ProductShippingCommandHandler(IDocumentStore documentStore,
        ILogger<ProductShippingCommandHandler> logger,
        TimeProvider dateTimeProvider,
        IPublishEndpoint publishEndpoint)
    {
        _documentStore = documentStore;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _publishEndpoint = publishEndpoint;
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

        DateTimeOffset occuredOnUtc = _dateTimeProvider.GetUtcNow();
        ProductShipped productShipped = new(request.Sku, request.Quantity, occuredOnUtc);
        session.Events.Append(productShipped.Sku, productShipped);
        await session.SaveChangesAsync(cancellationToken);

        await _publishEndpoint.Publish(new ProductShippedIntegrationEvent(request.Sku, request.Quantity, request.OrderId, occuredOnUtc), cancellationToken);

        _logger.LogDebug("Shipping product {Sku} with quantity {Quantity} succeeded.", request.Sku, request.Quantity);
        return Unit.Value;
    }
}