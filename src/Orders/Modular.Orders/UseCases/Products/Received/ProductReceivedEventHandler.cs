using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Common.Events;
using Modular.Orders.UseCases.Common;
using Modular.Warehouse.IntegrationEvents;

namespace Modular.Orders.UseCases.Products.Received;
internal class ProductReceivedEventHandler : IIntegrationEventConsumer<ProductQuantityIncreasedInWarehouseIntegrationEvent>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<ProductReceivedEventHandler> _logger;

    public ProductReceivedEventHandler(OrderDbContext orderDbContext, ILogger<ProductReceivedEventHandler> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task ConsumeAsync(ProductQuantityIncreasedInWarehouseIntegrationEvent message, CancellationToken cancellationToken)
    {
        Product? product = await _orderDbContext.Products.SingleOrDefaultAsync(p => p.SKU == message.Sku, cancellationToken);

        if (product is null)
        {
            _logger.LogError("Product {Sku} does not exists.", message.Sku);
            return;
        }

        product.IncreaseStock(message.Quantity);

        await _orderDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product {Sku} quantity is increased for {Quantiity}. Current quantity is {TotalAmmount}",
            message.Sku, message.Quantity, product.StockQuantity);
    }
}
