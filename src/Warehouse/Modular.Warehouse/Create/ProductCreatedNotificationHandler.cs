using Marten;
using MassTransit;
using Microsoft.Extensions.Logging;
using Modular.Catalog.IntegrationEvents;
using Modular.Warehouse.SourceModels;

namespace Modular.Warehouse.Create;

internal sealed class ProductCreatedNotificationHandler : IConsumer<ProductCreatedIntegrationEvent>
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

    public async Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
    {
        string sku = context.Message.Sku;
        _logger.LogInformation("Creating product {Sku}.", sku);

        await using var session = _documentStore.LightweightSession();
        Product? product = await session.Events.AggregateStreamAsync<Product>(sku);

        if (product is not null)
        {
            _logger.LogWarning("Product with SKU: {Sku} already exists.", sku);
            return;
        }

        ProductCreated productCreated = new(sku, context.Message.Name, _dateTimeProvider.GetUtcNow());
        session.Events.StartStream<Product>(sku, productCreated);
        await session.SaveChangesAsync();

        _logger.LogDebug("Creating product {Sku} succeeded.", sku);

    }

}
