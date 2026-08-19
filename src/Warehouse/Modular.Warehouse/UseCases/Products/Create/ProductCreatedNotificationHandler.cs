using Marten;
using Microsoft.Extensions.Logging;
using Modular.Catalog.IntegrationEvents;
using Modular.Common.Events;
using Modular.Warehouse.SourceModels;
using Modular.Warehouse.UseCases.Products.Models;

namespace Modular.Warehouse.UseCases.Products.Create;

internal sealed class ProductCreatedNotificationHandler : IIntegrationEventConsumer<ProductCreatedIntegrationEvent>
{
    private readonly IDocumentStore _documentStore;
    private readonly TimeProvider _dateTimeProvider;
    private readonly ILogger<ProductCreatedNotificationHandler> _logger;

    public ProductCreatedNotificationHandler(IDocumentStore documentStore, ILogger<ProductCreatedNotificationHandler> logger, TimeProvider dateTimeProvider)
    {
        _documentStore = documentStore;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task ConsumeAsync(ProductCreatedIntegrationEvent message, CancellationToken cancellationToken)
    {
        string sku = message.Sku;
        _logger.LogInformation("Creating product {Sku}.", sku);

        await using var session = _documentStore.LightweightSession();
        Product? product = await session.LoadAsync<Product>(sku, cancellationToken);

        if (product is not null)
        {
            _logger.LogWarning("Product with SKU: {Sku} already exists.", sku);
            return;
        }

        ProductCreated productCreated = new(sku, message.Name, _dateTimeProvider.GetUtcNow());
        session.Events.StartStream<Product>(sku, productCreated);
        await session.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Creating product {Sku} succeeded.", sku);

    }

}
