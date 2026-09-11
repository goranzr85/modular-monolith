using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Orders.UseCases.Orders.Change.RemoveProducts;
using Modular.Orders.UseCases.Orders.Create;
using Modular.Orders.UseCases.Orders.Models;
using Xunit;

namespace Modular.Orders.IntegrationTests;

[Collection(nameof(OrderDatabaseCollection))]
public sealed class RemoveProductCommandTests
{
    private readonly OrderDatabaseFixture _fixture;

    public RemoveProductCommandTests(OrderDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Guid OrderId, int ProductId)> SeedOrderWithItemAsync()
    {
        int productId = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await OrderTestHelpers.SeedProductAsync(sp.GetRequiredService<OrderDbContext>(), stockQuantity: 100));

        Guid orderId = Guid.NewGuid();

        ErrorOr<Guid> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
        {
            List<OrderItem> items = [new OrderItem { ProductId = productId, Quantity = 2, Price = Price.Create(9.99m).Value }];
            return await sp.GetRequiredService<CreateOrderCommandHandler>().Handle(new CreateOrderCommand(orderId, DateTimeOffset.UtcNow, Guid.NewGuid(), items), CancellationToken.None);
        });
        Assert.False(result.IsError);

        return (orderId, productId);
    }

    [Fact]
    public async Task Handle_WithExistingItem_RemovesItFromOrder()
    {
        (Guid orderId, int productId) = await SeedOrderWithItemAsync();

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<RemoveProductCommandHandler>().Handle(new RemoveProductCommand(orderId, productId), CancellationToken.None));

        Assert.False(result.IsError);

        Order? order = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking().Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId));
        Assert.NotNull(order);
        Assert.Empty(order.Items);
    }

    [Fact]
    public async Task Handle_WithUnknownOrder_ReturnsOrderNotFound()
    {
        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<RemoveProductCommandHandler>().Handle(new RemoveProductCommand(Guid.NewGuid(), 1), CancellationToken.None));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Order.NotFound", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithProductNotInOrder_ReturnsNotFound()
    {
        (Guid orderId, _) = await SeedOrderWithItemAsync();
        int otherProductId = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await OrderTestHelpers.SeedProductAsync(sp.GetRequiredService<OrderDbContext>(), stockQuantity: 100));

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<RemoveProductCommandHandler>().Handle(new RemoveProductCommand(orderId, otherProductId), CancellationToken.None));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Order.ProductIsNotPlaced", result.FirstError.Code);
    }
}
