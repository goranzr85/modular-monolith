using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Orders.UseCases.Common;
using Modular.Warehouse.IntegrationEvents;

namespace Modular.Orders.UseCases.Products.Received;
internal class ProductReceivedEventHandler : IConsumer<ProductQuantityIncreasedInWarehouseIntegrationEvent>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<ProductReceivedEventHandler> _logger;

    public ProductReceivedEventHandler(OrderDbContext orderDbContext, ILogger<ProductReceivedEventHandler> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductQuantityIncreasedInWarehouseIntegrationEvent> context)
    {
        Product? product = await _orderDbContext.Products.SingleOrDefaultAsync(p => p.SKU == context.Message.Sku);

        if (product is null)
        {
            _logger.LogError("Product {Sku} does not exists.", context.Message.Sku);
            return;
        }

        product.IncreaseStock(context.Message.Quantity);

        await _orderDbContext.SaveChangesAsync();

        _logger.LogInformation("Product {Sku} quantity is increased for {Quantiity}. Current quantity is {TotalAmmount}",
            context.Message.Sku, context.Message.Quantity, product.StockQuantity);
    }
}
