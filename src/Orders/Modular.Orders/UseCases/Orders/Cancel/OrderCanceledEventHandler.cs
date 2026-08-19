using Microsoft.EntityFrameworkCore;
using Modular.Common.Events;
using Modular.Orders.UseCases.Common;

namespace Modular.Orders.UseCases.Orders.Cancel;

internal sealed class OrderCanceledEventHandler : IIntegrationEventConsumer<OrderCanceledEvent>
{
    private readonly OrderDbContext _orderDbContext;

    public OrderCanceledEventHandler(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task ConsumeAsync(OrderCanceledEvent message, CancellationToken cancellationToken)
    {
        var results = await _orderDbContext.Orders
            .Where(x => x.Id == message.OrderId)
            .SelectMany(x => x.Items)
            .Select(x => new { x.ProductId, x.Quantity })
            .ToArrayAsync(cancellationToken);

        await using var transaction = await _orderDbContext.Database.BeginTransactionAsync(cancellationToken);

        int[] productIds = results.Select(x => x.ProductId).ToArray();

        Product[] products = await _orderDbContext.Products
            .FromSqlInterpolated($"SELECT * FROM \"Orders\".\"Products\" WHERE \"Id\" IN ({string.Join(",", productIds)}) FOR UPDATE")
            .ToArrayAsync(cancellationToken);

        foreach (var result in results)
        {
            Product product = products.Single(x => x.Id == result.ProductId);
            product.IncreaseStock(result.Quantity);
        }

        await _orderDbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
