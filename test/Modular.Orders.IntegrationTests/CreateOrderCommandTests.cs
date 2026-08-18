using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Orders.UseCases.Orders.Create;
using Modular.Orders.UseCases.Orders.Models;
using Xunit;

namespace Modular.Orders.IntegrationTests;

[Collection(nameof(OrderDatabaseCollection))]
public sealed class CreateOrderCommandTests
{
    private readonly OrderDatabaseFixture _fixture;

    public CreateOrderCommandTests(OrderDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_WithNewOrderIdAndItems_PersistsOrderWithPendingStatus()
    {
        int productId = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await OrderTestHelpers.SeedProductAsync(sp.GetRequiredService<OrderDbContext>(), stockQuantity: 100));

        Guid orderId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();

        // Note: Order.Create sums per-item Price directly (not Price * Quantity), so TotalAmount below
        // intentionally reflects that current behavior rather than an extended-price calculation.
        ErrorOr<Guid> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
        {
            ISender sender = sp.GetRequiredService<ISender>();
            List<OrderItem> items = [new OrderItem { ProductId = productId, Quantity = 3, Price = Price.Create(9.99m) }];
            return await sender.Send(new CreateOrderCommand(orderId, DateTimeOffset.UtcNow, customerId, items));
        });

        Assert.False(result.IsError);
        Assert.Equal(orderId, result.Value);

        Order? order = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId));

        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Pending.Name, order.Status.Name);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Single(order.Items);
        Assert.Equal(9.99m, (decimal)order.TotalAmount);
    }

    [Fact]
    public async Task Handle_WithAlreadyExistingOrderId_ReturnsValidationError()
    {
        int productId = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await OrderTestHelpers.SeedProductAsync(sp.GetRequiredService<OrderDbContext>(), stockQuantity: 100));

        Guid orderId = Guid.NewGuid();

        ErrorOr<Guid> first = await OrderTestHelpers.RunAsync(_fixture, async sp =>
        {
            List<OrderItem> items = [new OrderItem { ProductId = productId, Quantity = 1, Price = Price.Create(9.99m) }];
            return await sp.GetRequiredService<ISender>().Send(new CreateOrderCommand(orderId, DateTimeOffset.UtcNow, Guid.NewGuid(), items));
        });
        Assert.False(first.IsError);

        ErrorOr<Guid> second = await OrderTestHelpers.RunAsync(_fixture, async sp =>
        {
            List<OrderItem> items = [new OrderItem { ProductId = productId, Quantity = 1, Price = Price.Create(9.99m) }];
            return await sp.GetRequiredService<ISender>().Send(new CreateOrderCommand(orderId, DateTimeOffset.UtcNow, Guid.NewGuid(), items));
        });

        Assert.True(second.IsError);
        Assert.Equal(ErrorType.Validation, second.FirstError.Type);
        Assert.Equal("Order.OrderAlreadyCreated", second.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithEmptyItems_ThrowsArgumentException()
    {
        // Documents current behavior: Modular.Orders has no FluentValidation layer, so Order.Create's
        // guard clauses throw raw exceptions instead of the handler returning an ErrorOr validation error.
        await OrderTestHelpers.RunAsync(_fixture, async sp =>
        {
            ISender sender = sp.GetRequiredService<ISender>();
            CreateOrderCommand command = new(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), []);

            await Assert.ThrowsAsync<ArgumentException>(() => sender.Send(command));
        });
    }
}
