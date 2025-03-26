using MassTransit;
using Microsoft.EntityFrameworkCore;
using Modular.Orders.Change.AddProducts;
using Modular.Orders.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modular.Orders.DomainEventHandlers;

internal sealed class OrderItemAddedConsumer : IConsumer<OrderItemAddedEvent>
{
    private readonly OrderDbContext _orderDbContext;

    public OrderItemAddedConsumer(OrderDbContext orderDbContext)
    {
        _orderDbContext = orderDbContext;
    }

    public async Task Consume(ConsumeContext<OrderItemAddedEvent> context)
    {
        await using var transaction = await _orderDbContext.Database.BeginTransactionAsync();

        Product? product = await _orderDbContext.Products
            .FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {context.Message.ProductId} FOR UPDATE")
            .FirstOrDefaultAsync();

        if (product is null)
        {
            throw new InvalidOperationException($"Product with id {context.Message.ProductId} not found.");
        }

        product.DecreaseStock(context.Message.Quantity);

        await _orderDbContext.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}
