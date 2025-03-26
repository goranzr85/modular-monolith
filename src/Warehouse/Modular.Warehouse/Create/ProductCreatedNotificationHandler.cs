using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Catalog.IntegrationEvents;
using System.Threading;

namespace Modular.Warehouse.Create;

internal sealed class ProductCreatedNotificationHandler : IConsumer<ProductCreatedIntegrationEvent>
{
    private readonly WarehouseDbContext _warehouseDbContext;
    private readonly ILogger<ProductCreatedNotificationHandler> _logger;

    public ProductCreatedNotificationHandler(WarehouseDbContext warehouseDbContext, ILogger<ProductCreatedNotificationHandler> logger)
    {
        _warehouseDbContext = warehouseDbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
    {
        ProductCreatedIntegrationEvent productCreatedEvent = context.Message;

        Product? product = await _warehouseDbContext.Products
            .FirstOrDefaultAsync(p => p.Sku == productCreatedEvent.Sku, CancellationToken.None);

        if (product is not null)
        {
            _logger.LogWarning("Product with SKU: {Sku} already exists", productCreatedEvent.Sku);
            return;
        }

        product = Product.Create(productCreatedEvent.Sku);

        await _warehouseDbContext.AddAsync(product, CancellationToken.None);
        await _warehouseDbContext.SaveChangesAsync(CancellationToken.None);
    }

}
