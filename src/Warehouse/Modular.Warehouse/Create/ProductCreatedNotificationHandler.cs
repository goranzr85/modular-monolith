using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.ProductIntegrationEvents;

namespace Modular.Warehouse.Create;
internal class ProductCreatedNotificationHandler : INotificationHandler<ProductCreatedIntegrationEvent>
{
    private readonly WarehouseDbContext _warehouseDbContext;
    private readonly ILogger<ProductCreatedNotificationHandler> _logger;

    public ProductCreatedNotificationHandler(WarehouseDbContext warehouseDbContext, ILogger<ProductCreatedNotificationHandler> logger)
    {
        _warehouseDbContext = warehouseDbContext;
        _logger = logger;
    }

    public async Task Handle(ProductCreatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        Product? product = await _warehouseDbContext.Products
            .FirstOrDefaultAsync(p => p.Sku == notification.Sku, cancellationToken);

        if (product is not null)
        {
            _logger.LogWarning("Product with SKU: {Sku} already exists", notification.Sku);
            return;
        }

        product = Product.Create(notification.Sku);

        await _warehouseDbContext.AddAsync(product, cancellationToken);
        await _warehouseDbContext.SaveChangesAsync(cancellationToken);
    }
}
