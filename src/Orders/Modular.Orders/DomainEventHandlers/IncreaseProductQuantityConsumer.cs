using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modular.Orders.Change.AddProducts;
using Modular.Orders.Change.ChangeProductQuantity.Decrease;
using Modular.Orders.Models;

namespace Modular.Orders.DomainEventHandlers;

internal sealed class IncreaseProductQuantityConsumer : IConsumer<OrderItemAddedEvent>,
    IConsumer<DecreasedProductQuantityInOrderEvent>
{
    private readonly OrderDbContext _orderDbContext;
    private readonly ILogger<IncreaseProductQuantityConsumer> _logger;

    public IncreaseProductQuantityConsumer(OrderDbContext orderDbContext, ILogger<IncreaseProductQuantityConsumer> logger)
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

            product.IncreaseStock(quantity);

            await _orderDbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "An error occurred while increase product quantity for product {ProductId} by {Quantity}.", productId, quantity);
        }
    }

}
