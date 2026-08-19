using Microsoft.EntityFrameworkCore;
using Modular.Common.Events;
using Modular.Orders.UseCases.Common;
using Modular.Orders.UseCases.Orders.Models;

namespace Modular.Orders.UseCases.Orders.Create;

internal sealed class OrderCreatedEventHandler : IIntegrationEventConsumer<OrderCreatedEvent>
{
    private readonly OrderDbContext _orderDbContext;

    public OrderCreatedEventHandler(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task ConsumeAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        int[] productIds = message.OrderItems
            .Select(x => x.ProductId)
            .ToArray();

        await using var transaction = await _orderDbContext.Database.BeginTransactionAsync(cancellationToken);

        Product[] products = await _orderDbContext.Products
            .FromSqlInterpolated($"SELECT * FROM \"Orders\".\"Products\" WHERE \"Id\" IN ({string.Join(",", productIds)}) FOR UPDATE")
            .ToArrayAsync(cancellationToken);

        foreach (OrderItem orderItem in message.OrderItems)
        {
            Product product = products.Single(x => x.Id == orderItem.ProductId);
            product.DecreaseStock(orderItem.Quantity);
        }

        await _orderDbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
