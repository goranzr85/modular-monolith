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
public sealed class OrderSubmitCommandTests
{
    private readonly OrderDatabaseFixture _fixture;

    public OrderSubmitCommandTests(OrderDatabaseFixture fixture)
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
    public async Task Handle_WithPendingOrder_SubmitsSuccessfully()
    {
        Guid orderId = await SeedOrderAsync();

        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new OrderSubmitCommand(orderId)));

        Assert.False(result.IsError);

        Order? order = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId));

        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Submitted.Name, order.Status.Name);
        Assert.NotNull(order.SubmittedDate);
    }

    [Fact]
    public async Task Handle_WithUnknownOrderId_ReturnsNotFound()
    {
        ErrorOr<Unit> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new OrderSubmitCommand(Guid.NewGuid())));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Order.NotFound", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithAlreadySubmittedOrder_ReturnsIllegalTransitionError()
    {
        Guid orderId = await SeedOrderAsync();

        ErrorOr<Unit> first = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new OrderSubmitCommand(orderId)));
        Assert.False(first.IsError);

        ErrorOr<Unit> second = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new OrderSubmitCommand(orderId)));

        Assert.True(second.IsError);
        Assert.Equal("Order.OrderStatusIllegalTransition", second.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithCanceledOrder_ReturnsIllegalTransitionError()
    {
        Guid orderId = await SeedOrderAsync();

        ErrorOr<Unit> cancel = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new CancelOrderCommand(orderId)));
        Assert.False(cancel.IsError);

        ErrorOr<Unit> submit = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<ISender>().Send(new OrderSubmitCommand(orderId)));

        Assert.True(submit.IsError);
        Assert.Equal("Order.OrderStatusIllegalTransition", submit.FirstError.Code);
    }
}
