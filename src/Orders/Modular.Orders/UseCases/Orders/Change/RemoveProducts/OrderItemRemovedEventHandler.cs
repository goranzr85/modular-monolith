using Microsoft.EntityFrameworkCore;
using Modular.Common.Events;
using Modular.Orders.UseCases.Common;

namespace Modular.Orders.UseCases.Orders.Change.RemoveProducts;

internal sealed class OrderItemRemovedEventHandler : IIntegrationEventConsumer<OrderItemRemovedEvent>
{
    private readonly OrderDbContext _orderDbContext;

    public OrderItemRemovedEventHandler(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task ConsumeAsync(OrderItemRemovedEvent message, CancellationToken cancellationToken)
    {
        int productId = message.ProductId;

        await using var transaction = await _orderDbContext.Database.BeginTransactionAsync(cancellationToken);

        Product product = await _orderDbContext.Products
            .FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {productId} FOR UPDATE")
            .FirstAsync(cancellationToken);

        product.IncreaseStock(message.Quantity);

        await _orderDbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
