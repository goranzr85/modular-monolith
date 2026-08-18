using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Decrease;
using Modular.Orders.UseCases.Orders.Create;
using Modular.Orders.UseCases.Orders.Models;
using Xunit;

namespace Modular.Orders.IntegrationTests;

[Collection(nameof(OrderDatabaseCollection))]
public sealed class DecreaseProductQuantityCommandTests
{
    private readonly OrderDatabaseFixture _fixture;

    public DecreaseProductQuantityCommandTests(OrderDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Guid OrderId, int ProductId)> SeedOrderWithItemAsync(uint initialQuantity)
    {
        int productId = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await OrderTestHelpers.SeedProductAsync(sp.GetRequiredService<OrderDbContext>(), stockQuantity: 100));

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
    public async Task Handle_WithEnoughQuantity_SetsQuantityToRequestedAmountInsteadOfSubtractingIt()
    {
        // Documents a known bug: Order.DecreaseQuantity computes existingOrderItem.DecreaseQuantity(
        // existingOrderItem.Quantity - quantity), and OrderItem.DecreaseQuantity itself does another "-=",
        // so under modular (uint) arithmetic the two subtractions cancel out and the final quantity is
        // exactly `quantity` (the requested decrease amount) rather than existing - quantity.
        // Confirmed with the project owner (2026-08-18) to leave the source as-is and pin this via tests.
        (Guid orderId, int productId) = await SeedOrderWithItemAsync(initialQuantity: 10);

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new DecreaseProductQuantityCommand(orderId, productId, 4)));

        Assert.False(result.IsError);

        Order? order = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking().Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId));
        Assert.NotNull(order);
        OrderItem item = order.Items.Single(i => i.ProductId == productId);

        Assert.Equal(4u, item.Quantity);
    }

    [Fact]
    public async Task Handle_WithMoreThanAvailableQuantity_ReturnsValidationErrorAndDoesNotChangeQuantity()
    {
        (Guid orderId, int productId) = await SeedOrderWithItemAsync(initialQuantity: 10);

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new DecreaseProductQuantityCommand(orderId, productId, 15)));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal("Order.ProductQuantityIsNotEnoughForDecrease", result.FirstError.Code);

        Order? order = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking().Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId));
        Assert.NotNull(order);
        Assert.Equal(10u, order.Items.Single(i => i.ProductId == productId).Quantity);
    }

    [Fact]
    public async Task Handle_WithProductNotPlacedInOrder_ReturnsNotFoundError()
    {
        (Guid orderId, _) = await SeedOrderWithItemAsync(initialQuantity: 10);
        int otherProductId = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await OrderTestHelpers.SeedProductAsync(sp.GetRequiredService<OrderDbContext>(), stockQuantity: 100));

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new DecreaseProductQuantityCommand(orderId, otherProductId, 1)));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Order.ProductIsNotPlaced", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithUnknownOrder_ReturnsOrderNotFound()
    {
        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new DecreaseProductQuantityCommand(Guid.NewGuid(), 1, 1)));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Order.NotFound", result.FirstError.Code);
    }
}
