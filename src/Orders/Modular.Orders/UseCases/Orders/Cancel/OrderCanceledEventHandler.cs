using MassTransit;
using Microsoft.EntityFrameworkCore;
using Modular.Orders.UseCases.Common;

namespace Modular.Orders.UseCases.Orders.Cancel;

internal sealed class OrderCanceledEventHandler : IConsumer<OrderCanceledEvent>
{
    private readonly OrderDbContext _orderDbContext;

    public OrderCanceledEventHandler(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task Consume(ConsumeContext<OrderCanceledEvent> context)
    {
        var results = await _orderDbContext.Orders
            .Where(x => x.Id == context.Message.OrderId)
            .SelectMany(x => x.Items)
            .Select(x => new { x.ProductId, x.Quantity })
            .ToArrayAsync();

        await using var transaction = await _orderDbContext.Database.BeginTransactionAsync();

        int[] productIds = results.Select(x => x.ProductId).ToArray();

        Product[] products = await _orderDbContext.Products
            .FromSqlInterpolated($"SELECT * FROM Products WHERE Id IN ({string.Join(",", productIds)}) FOR UPDATE")
            .ToArrayAsync();

        foreach (var result in results)
        {
            Product product = products.Single(x => x.Id == result.ProductId);
            product.IncreaseStock(result.Quantity);
        }

        await _orderDbContext.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}
