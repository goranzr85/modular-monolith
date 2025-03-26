using MassTransit;
using Microsoft.EntityFrameworkCore;
using Modular.Orders.Change.RemoveProducts;
using Modular.Orders.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modular.Orders.EventHandlersConsumers;

internal sealed class OrderItemRemovedEventHandler : IConsumer<OrderItemRemoved>
{
    private readonly OrderDbContext _orderDbContext;

    public OrderItemRemovedEventHandler(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task Consume(ConsumeContext<OrderItemRemoved> context)
    {
        int productId = context.Message.ProductId;

        await using var transaction = await _orderDbContext.Database.BeginTransactionAsync();

        Product product = await _orderDbContext.Products
            .FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {productId} FOR UPDATE")
            .FirstAsync();

        product.IncreaseStock(context.Message.Quantity);

        await _orderDbContext.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}
