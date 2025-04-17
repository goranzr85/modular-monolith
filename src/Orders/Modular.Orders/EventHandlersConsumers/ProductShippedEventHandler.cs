using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Orders.Models;
using Modular.Warehouse.IntegrationEvents;

namespace Modular.Orders.EventHandlersConsumers;
internal class ProductShippedEventHandler : IConsumer<ProductShippedIntegrationEvent>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<ProductShippedEventHandler> _logger;

    public ProductShippedEventHandler(OrderDbContext orderDbContext, ILogger<ProductShippedEventHandler> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductShippedIntegrationEvent> context)
    {
        Product? product = await _orderDbContext.Products.SingleOrDefaultAsync(p => p.SKU == context.Message.Sku);

        if (product is null)
        {
            _logger.LogError("Product {Sku} does not exists.", context.Message.Sku);
            return;
        }

        product.DecreaseStock(context.Message.Quantity);

        await _orderDbContext.SaveChangesAsync();

        _logger.LogInformation("Product {Sku} quantity is decreased for {Quantiity}. Current quantity is {TotalAmmount}",
            context.Message.Sku, context.Message.Quantity, product.StockQuantity);
    }
}
