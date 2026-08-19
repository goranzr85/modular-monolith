using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Orders.UseCases.Orders.Change.AddProducts;
using Modular.Orders.UseCases.Orders.Create;
using Modular.Orders.UseCases.Orders.Models;
using Xunit;

namespace Modular.Orders.IntegrationTests;

[Collection(nameof(OrderDatabaseCollection))]
public sealed class AddProductCommandTests
{
    private readonly OrderDatabaseFixture _fixture;

    public AddProductCommandTests(OrderDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Guid> SeedOrderAsync(int seedProductId)
    {
        Guid orderId = Guid.NewGuid();

        ErrorOr<Guid> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
        {
            List<OrderItem> items = [new OrderItem { ProductId = seedProductId, Quantity = 1, Price = Price.Create(9.99m) }];
            return await sp.GetRequiredService<CreateOrderCommandHandler>().Handle(new CreateOrderCommand(orderId, DateTimeOffset.UtcNow, Guid.NewGuid(), items), CancellationToken.None);
        });
        Assert.False(result.IsError);

        return orderId;
    }

    private Task<int> SeedProductAsync(uint stockQuantity) =>
        OrderTestHelpers.RunAsync(_fixture, async sp =>
            await OrderTestHelpers.SeedProductAsync(sp.GetRequiredService<OrderDbContext>(), stockQuantity));

    [Fact]
    public async Task Handle_WithNewProductAndSufficientStock_AddsItemToOrder()
    {
        int seedProductId = await SeedProductAsync(stockQuantity: 100);
        Guid orderId = await SeedOrderAsync(seedProductId);
        int newProductId = await SeedProductAsync(stockQuantity: 50);

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<AddProductCommandHandler>().Handle(new AddProductCommand(orderId, newProductId, 5, Price.Create(14.5m)), CancellationToken.None));

        Assert.False(result.IsError);

        Order? order = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking().Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId));

        Assert.NotNull(order);
        Assert.Equal(2, order.Items.Count);
        OrderItem? addedItem = order.Items.FirstOrDefault(i => i.ProductId == newProductId);
        Assert.NotNull(addedItem);
        Assert.Equal(5u, addedItem.Quantity);
    }

    [Fact]
    public async Task Handle_WithProductNotInStock_ReturnsValidationErrorAndDoesNotAddItem()
    {
        int seedProductId = await SeedProductAsync(stockQuantity: 100);
        Guid orderId = await SeedOrderAsync(seedProductId);
        int lowStockProductId = await SeedProductAsync(stockQuantity: 2);

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<AddProductCommandHandler>().Handle(new AddProductCommand(orderId, lowStockProductId, 5, Price.Create(14.5m)), CancellationToken.None));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal("Order.NotEnoughProductQuantity", result.FirstError.Code);

        Order? order = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking().Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId));
        Assert.NotNull(order);
        Assert.DoesNotContain(order.Items, i => i.ProductId == lowStockProductId);
    }

    [Fact]
    public async Task Handle_WithNonExistentProduct_ReturnsProductNotFound()
    {
        int seedProductId = await SeedProductAsync(stockQuantity: 100);
        Guid orderId = await SeedOrderAsync(seedProductId);

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<AddProductCommandHandler>().Handle(new AddProductCommand(orderId, int.MaxValue, 1, Price.Create(9.99m)), CancellationToken.None));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Order.ProductNotFound", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithUnknownOrder_ReturnsOrderNotFound()
    {
        int productId = await SeedProductAsync(stockQuantity: 100);

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<AddProductCommandHandler>().Handle(new AddProductCommand(Guid.NewGuid(), productId, 1, Price.Create(9.99m)), CancellationToken.None));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Order.NotFound", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_ReAddingExistingProduct_SetsQuantityToLastRequestedAmountInsteadOfAddingToIt()
    {
        // Documents a known bug: Order.AddItem's existing-item branch computes
        // existingOrderItem.IncreaseQuantity(quantity - existingOrderItem.Quantity), and because that
        // subtraction and the += inside OrderItem.IncreaseQuantity are both unsigned (uint) and cancel
        // out under modular arithmetic, the net effect is Quantity := quantity - i.e. re-adding a product
        // always *replaces* the line quantity with the newly requested amount instead of adding to it.
        // Confirmed with the project owner (2026-08-18) to leave the source as-is and pin this via tests.
        int seedProductId = await SeedProductAsync(stockQuantity: 1000);
        Guid orderId = await SeedOrderAsync(seedProductId);

        ErrorOr<Unit> firstAdd = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<AddProductCommandHandler>().Handle(new AddProductCommand(orderId, seedProductId, 10, Price.Create(9.99m)), CancellationToken.None));
        Assert.False(firstAdd.IsError);

        ErrorOr<Unit> secondAdd = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<AddProductCommandHandler>().Handle(new AddProductCommand(orderId, seedProductId, 3, Price.Create(9.99m)), CancellationToken.None));
        Assert.False(secondAdd.IsError);

        Order? order = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking().Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId));
        Assert.NotNull(order);
        OrderItem item = order.Items.Single(i => i.ProductId == seedProductId);

        Assert.Equal(3u, item.Quantity);
    }
}
