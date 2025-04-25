using MassTransit;
using Microsoft.EntityFrameworkCore;
using Modular.Orders.UseCases.Common;

namespace Modular.Orders.UseCases.Orders.Change.AddProducts;

internal sealed class OrderItemAddedEventHandler : IConsumer<OrderItemAddedEvent>
{
    private readonly OrderDbContext _orderDbContext;

    public OrderItemAddedEventHandler(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task Consume(ConsumeContext<OrderItemAddedEvent> context)
    {
        int productId = context.Message.ProductId;

        await using var transaction = await _orderDbContext.Database.BeginTransactionAsync();

        Product product = await _orderDbContext.Products
            .FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {productId} FOR UPDATE")
            .FirstAsync();

        product.DecreaseStock(context.Message.Quantity);

        await _orderDbContext.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}
