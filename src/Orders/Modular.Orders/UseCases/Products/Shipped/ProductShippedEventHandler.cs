//using MassTransit;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Modular.Orders.UseCases.Common;
//using Modular.Orders.UseCases.Orders.Models;
//using Modular.Warehouse.IntegrationEvents;

//namespace Modular.Orders.UseCases.Products.Shipped;
//internal class ProductShippedEventHandler : IConsumer<ProductShippedIntegrationEvent>
//{
//    private readonly OrderDbContext _orderDbContext;
//    private readonly ILogger<ProductShippedEventHandler> _logger;

//    public ProductShippedEventHandler(OrderDbContext orderDbContext, ILogger<ProductShippedEventHandler> logger)
//    {
//        _orderDbContext = orderDbContext;
//        _logger = logger;
//    }

//    public async Task Consume(ConsumeContext<ProductShippedIntegrationEvent> context)
//    {
//        Order? order = await _orderDbContext.Orders.SingleOrDefaultAsync(p => p.Id== context.Message.OrderId);

//        if (order is null)
//        {
//            _logger.LogError("Order {OrderId} does not exists.", context.Message.OrderId);
//            return;
//        }

//        order.MarkItemAsShipped

//        product.DecreaseStock(context.Message.Quantity);

//        await _orderDbContext.SaveChangesAsync();

//        _logger.LogInformation("Product {Sku} quantity is decreased for {Quantiity}. Current quantity is {TotalAmmount}",
//            context.Message.Sku, context.Message.Quantity, product.StockQuantity);
//    }
//}
