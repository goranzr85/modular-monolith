using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Increase;
using Modular.Orders.UseCases.Orders.Create;
using Modular.Orders.UseCases.Orders.Models;
using Xunit;

namespace Modular.Orders.IntegrationTests;

[Collection(nameof(OrderDatabaseCollection))]
public sealed class IncreaseProductQuantityCommandTests
{
    private readonly OrderDatabaseFixture _fixture;

    public IncreaseProductQuantityCommandTests(OrderDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Guid OrderId, int ProductId)> SeedOrderWithItemAsync(uint initialQuantity, uint stockQuantity)
    {
        int productId = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await OrderTestHelpers.SeedProductAsync(sp.GetRequiredService<OrderDbContext>(), stockQuantity));

        Guid orderId = Guid.NewGuid();

        ErrorOr<Guid> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
        {
            List<OrderItem> items = [new OrderItem { ProductId = productId, Quantity = initialQuantity, Price = Price.Create(9.99m) }];
            return await sp.GetRequiredService<ISender>().Send(new CreateOrderCommand(orderId, DateTimeOffset.UtcNow, Guid.NewGuid(), items));
        });
        Assert.False(result.IsError);

        return (orderId, productId);
    }

    [Fact]
    public async Task Handle_WithSufficientStock_DoublesExistingQuantityPlusRequested()
    {
        // Documents a known bug: Order.IncreaseQuantity computes existingOrderItem.IncreaseQuantity(quantity
        // + existingOrderItem.Quantity), and OrderItem.IncreaseQuantity itself does another "+=", so the
        // final quantity is 2*existing + requested instead of existing + requested.
        // Confirmed with the project owner (2026-08-18) to leave the source as-is and pin this via tests.
        (Guid orderId, int productId) = await SeedOrderWithItemAsync(initialQuantity: 5, stockQuantity: 100);

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new IncreaseProductQuantityCommand(orderId, productId, 3)));

        Assert.False(result.IsError);

        Order? order = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking().Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId));
        Assert.NotNull(order);
        OrderItem item = order.Items.Single(i => i.ProductId == productId);

        Assert.Equal(13u, item.Quantity);
    }

    [Fact]
    public async Task Handle_WithProductNotPlacedInOrder_ReturnsNotFoundError()
    {
        (Guid orderId, _) = await SeedOrderWithItemAsync(initialQuantity: 5, stockQuantity: 100);
        int otherProductId = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await OrderTestHelpers.SeedProductAsync(sp.GetRequiredService<OrderDbContext>(), stockQuantity: 100));

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new IncreaseProductQuantityCommand(orderId, otherProductId, 3)));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Order.ProductIsNotPlaced", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithInsufficientStock_ReturnsValidationError()
    {
        (Guid orderId, int productId) = await SeedOrderWithItemAsync(initialQuantity: 5, stockQuantity: 2);

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new IncreaseProductQuantityCommand(orderId, productId, 5)));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal("Order.NotEnoughProductQuantity", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithUnknownOrder_ReturnsOrderNotFound()
    {
        int productId = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await OrderTestHelpers.SeedProductAsync(sp.GetRequiredService<OrderDbContext>(), stockQuantity: 100));

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new IncreaseProductQuantityCommand(Guid.NewGuid(), productId, 1)));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Order.NotFound", result.FirstError.Code);
    }
}
