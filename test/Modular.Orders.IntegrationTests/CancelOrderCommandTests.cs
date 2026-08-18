using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Orders.UseCases.Orders.Cancel;
using Modular.Orders.UseCases.Orders.Create;
using Modular.Orders.UseCases.Orders.Models;
using Modular.Orders.UseCases.Orders.Submitted;
using Xunit;

namespace Modular.Orders.IntegrationTests;

[Collection(nameof(OrderDatabaseCollection))]
public sealed class CancelOrderCommandTests
{
    private readonly OrderDatabaseFixture _fixture;

    public CancelOrderCommandTests(OrderDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Guid> SeedOrderAsync()
    {
        int productId = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await OrderTestHelpers.SeedProductAsync(sp.GetRequiredService<OrderDbContext>(), stockQuantity: 100));

        Guid orderId = Guid.NewGuid();

        ErrorOr<Guid> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
        {
            List<OrderItem> items = [new OrderItem { ProductId = productId, Quantity = 1, Price = Price.Create(9.99m) }];
            return await sp.GetRequiredService<ISender>().Send(new CreateOrderCommand(orderId, DateTimeOffset.UtcNow, Guid.NewGuid(), items));
        });
        Assert.False(result.IsError);

        return orderId;
    }

    [Fact]
    public async Task Handle_WithPendingOrder_CancelsSuccessfully()
    {
        // Regression test: OrderStatus.PendingStatus.ChangeStatus previously had a pattern-matching
        // precedence bug ("status is not SubmittedStatus or CanceledStatus") that made every transition
        // except Pending->Submitted fail, so Cancel() from Pending always returned an error.
        Guid orderId = await SeedOrderAsync();

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new CancelOrderCommand(orderId)));

        Assert.False(result.IsError);

        Order? order = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId));

        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Canceled.Name, order.Status.Name);
        Assert.NotNull(order.CanceledDate);
    }

    [Fact]
    public async Task Handle_WithSubmittedOrder_CancelsSuccessfully()
    {
        Guid orderId = await SeedOrderAsync();

        ErrorOr<Unit> submitResult = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new OrderSubmitCommand(orderId)));
        Assert.False(submitResult.IsError);

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new CancelOrderCommand(orderId)));

        Assert.False(result.IsError);

        Order? order = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId));

        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Canceled.Name, order.Status.Name);
    }

    [Fact]
    public async Task Handle_WithUnknownOrderId_ReturnsNotFound()
    {
        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new CancelOrderCommand(Guid.NewGuid())));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Order.NotFound", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithAlreadyCanceledOrder_ReturnsIllegalTransitionError()
    {
        Guid orderId = await SeedOrderAsync();

        ErrorOr<Unit> firstCancel = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new CancelOrderCommand(orderId)));
        Assert.False(firstCancel.IsError);

        ErrorOr<Unit> secondCancel = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new CancelOrderCommand(orderId)));

        Assert.True(secondCancel.IsError);
        Assert.Equal("Order.OrderStatusIllegalTransition", secondCancel.FirstError.Code);
    }
}
