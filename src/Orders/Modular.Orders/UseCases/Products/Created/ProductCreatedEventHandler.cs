using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Catalog.IntegrationEvents;
using Modular.Orders.UseCases.Common;

namespace Modular.Orders.UseCases.Products.Created;

internal sealed class ProductCreatedEventHandler : IConsumer<ProductCreatedIntegrationEvent>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(OrderDbContext orderDbContext, ILogger<ProductCreatedEventHandler> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
    {
        Product product = Product.Create(context.Message.Sku, context.Message.Name, context.Message.Description, context.Message.Price);

        if (product is null)
        {
            _logger.LogError("Product {Sku} not created.", context.Message.Sku);
            return;
        }

        bool productAlreadyExist = await _orderDbContext.Products.AnyAsync(p => p.SKU == context.Message.Sku);

        if (productAlreadyExist)
        {
            _logger.LogError("Product {Sku} already exists.", context.Message.Sku);
            return;
        }

        await _orderDbContext.Products.AddAsync(product);
        await _orderDbContext.SaveChangesAsync();

        _logger.LogInformation("Product created: {Sku}.", context.Message.Sku);
    }
}