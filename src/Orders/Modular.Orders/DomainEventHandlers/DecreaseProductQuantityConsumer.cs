using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Orders.Change.AddProducts;
using Modular.Orders.Change.ChangeProductQuantity.Decrease;
using Modular.Orders.Change.ChangeProductQuantity.Increase;
using Modular.Orders.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modular.Orders.DomainEventHandlers;

internal sealed class DecreaseProductQuantityConsumer : IConsumer<OrderItemAddedEvent>,
    IConsumer<DecreasedProductQuantityInOrderEvent>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<DecreaseProductQuantityConsumer> _logger;

    public DecreaseProductQuantityConsumer(OrderDbContext orderDbContext, ILogger<DecreaseProductQuantityConsumer> logger)
    {
        _orderDbContext = orderDbContext;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderItemAddedEvent> context) =>
        DecreaseProductQuantity(context.Message.ProductId, context.Message.Quantity);

    public Task Consume(ConsumeContext<DecreasedProductQuantityInOrderEvent> context) =>
        DecreaseProductQuantity(context.Message.ProductId, context.Message.Quantity);

    private async Task DecreaseProductQuantity(int productId, uint quantity)
    {
        await using var transaction = await _orderDbContext.Database.BeginTransactionAsync();

        try
        {
            Product? product = await _orderDbContext.Products
                .FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {productId} FOR UPDATE")
                .FirstOrDefaultAsync();

            if (product is null)
            {
                throw new InvalidOperationException($"Product with id {productId} not found.");
            }

            product.DecreaseStock(quantity);

            await _orderDbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        catch (Exception)
        {
            await transaction.RollbackAsync();
            _logger.LogError("An error occurred while decreasing product quantity for product {ProductId} by {Quantity}.", productId, quantity);
        }
    }

}
