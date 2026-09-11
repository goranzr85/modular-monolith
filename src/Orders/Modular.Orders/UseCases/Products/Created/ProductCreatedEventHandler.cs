using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Catalog.IntegrationEvents;
using Modular.Common.Events;
using Modular.Orders.UseCases.Common;

namespace Modular.Orders.UseCases.Products.Created;

internal sealed class ProductCreatedEventHandler : IIntegrationEventConsumer<ProductCreatedIntegrationEvent>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(OrderDbContext orderDbContext, ILogger<ProductCreatedEventHandler> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public async Task ConsumeAsync(ProductCreatedIntegrationEvent message, CancellationToken cancellationToken)
    {
        ErrorOr<Product> productResult = Product.Create(message.Sku, message.Name, message.Description, message.Price);

        if (productResult.IsError)
        {
            _logger.LogError("Product {Sku} not created: {Error}.", message.Sku, productResult.FirstError.Description);
            return;
        }

        Product product = productResult.Value;

        bool productAlreadyExist = await _orderDbContext.Products.AnyAsync(p => p.SKU == message.Sku, cancellationToken);

        if (productAlreadyExist)
        {
            _logger.LogError("Product {Sku} already exists.", message.Sku);
            return;
        }

        await _orderDbContext.Products.AddAsync(product, cancellationToken);
        await _orderDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created: {Sku}.", message.Sku);
    }
}