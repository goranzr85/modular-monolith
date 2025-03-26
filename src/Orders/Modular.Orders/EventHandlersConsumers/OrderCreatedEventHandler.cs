using MassTransit;
using Microsoft.EntityFrameworkCore;
using Modular.Orders.Create;
using Modular.Orders.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modular.Orders.EventHandlersConsumers;

internal sealed class OrderCreatedEventHandler : IConsumer<OrderCreated>
{
    private readonly OrderDbContext _orderDbContext;

    public OrderCreatedEventHandler(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        int[] productIds = context.Message.OrderItems
            .Select(x => x.ProductId)
            .ToArray();

        await using var transaction = await _orderDbContext.Database.BeginTransactionAsync();

        Product[] products = await _orderDbContext.Products
            .FromSqlInterpolated($"SELECT * FROM Products WHERE Id IN ({string.Join(",", productIds)}) FOR UPDATE")
            .ToArrayAsync();

        foreach (OrderItem orderItem in context.Message.OrderItems)
        {
            Product product = products.Single(x => x.Id == orderItem.ProductId);
            product.DecreaseStock(orderItem.Quantity);
        }

        await _orderDbContext.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}
