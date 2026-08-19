using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Orders.Integrations;
using Modular.Orders.UseCases.Orders.Create;
using Modular.Orders.UseCases.Orders.Models;
using Modular.Orders.UseCases.Orders.Submitted;
using Modular.Payments.IntegrationEvents;
using Modular.Warehouse.IntegrationEvents;
using Xunit;

namespace Modular.Orders.IntegrationTests;

[Collection(nameof(OrderDatabaseCollection))]
public sealed class OrderSubmittedOrchestrationTests
{
    private readonly OrderDatabaseFixture _fixture;

    public OrderSubmittedOrchestrationTests(OrderDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Guid OrderId, int ProductId, string Sku)> SeedOrderWithSingleItemAsync(uint quantity = 1)
    {
        int productId = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await OrderTestHelpers.SeedProductAsync(sp.GetRequiredService<OrderDbContext>(), stockQuantity: 100));

        string sku = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            (await sp.GetRequiredService<OrderDbContext>().Products.AsNoTracking().SingleAsync(p => p.Id == productId)).SKU);

        Guid orderId = Guid.NewGuid();

        ErrorOr<Guid> result = await OrderTestHelpers.RunAsync(_fixture, async sp =>
        {
            List<OrderItem> items = [new OrderItem { ProductId = productId, Quantity = quantity, Price = Price.Create(9.99m) }];
            return await sp.GetRequiredService<CreateOrderCommandHandler>()
                .Handle(new CreateOrderCommand(orderId, DateTimeOffset.UtcNow, Guid.NewGuid(), items), CancellationToken.None);
        });
        Assert.False(result.IsError);

        return (orderId, productId, sku);
    }

    [Fact]
    public async Task ConsumeAsync_OrderSubmittedEvent_PublishesProcessPaymentForSameOrder()
    {
        Guid orderId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();
        OrderSubmittedEvent submittedEvent = new(orderId, customerId, Price.Create(19.99m));

        await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderSubmittedOrchestration>().ConsumeAsync(submittedEvent, CancellationToken.None));

        FakeIntegrationEventPublisher publisher = _fixture.Services.GetRequiredService<FakeIntegrationEventPublisher>();
        ProcessPayment? processPayment = publisher.Published<ProcessPayment>().SingleOrDefault(p => p.OrderId == orderId);

        Assert.NotNull(processPayment);
        Assert.Equal(customerId, processPayment.CustomerId);
        Assert.Equal(19.99m, processPayment.TotalAmount.Value);
    }

    [Fact]
    public async Task ConsumeAsync_PaymentProcessedIntegrationEvent_PublishesShipProductWithOrderItems()
    {
        // Regression test: this handler previously had no .Include(o => o.Items) at all, so order.Items was
        // always the constructor's empty default list and every ShipProduct went out with zero products.
        (Guid orderId, _, string sku) = await SeedOrderWithSingleItemAsync(quantity: 3);

        await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderSubmittedOrchestration>()
                .ConsumeAsync(new PaymentProcessedIntegrationEvent(orderId), CancellationToken.None));

        FakeIntegrationEventPublisher publisher = _fixture.Services.GetRequiredService<FakeIntegrationEventPublisher>();
        ShipProduct? shipProduct = publisher.Published<ShipProduct>().SingleOrDefault(p => p.OrderId == orderId);

        Assert.NotNull(shipProduct);
        Assert.Single(shipProduct.Products);
        Assert.Equal(sku, shipProduct.Products[0].ProductSku);
        Assert.Equal(3u, shipProduct.Products[0].Quantity);
    }

    [Fact]
    public async Task ConsumeAsync_PaymentProcessedIntegrationEvent_WithUnknownOrder_DoesNotPublishOrThrow()
    {
        Guid unknownOrderId = Guid.NewGuid();

        await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderSubmittedOrchestration>()
                .ConsumeAsync(new PaymentProcessedIntegrationEvent(unknownOrderId), CancellationToken.None));

        FakeIntegrationEventPublisher publisher = _fixture.Services.GetRequiredService<FakeIntegrationEventPublisher>();
        Assert.DoesNotContain(publisher.Published<ShipProduct>(), p => p.OrderId == unknownOrderId);
    }

    [Fact]
    public async Task ConsumeAsync_ProductShippedIntegrationEvent_MarksItemShippedAndPersists()
    {
        // Regression test: this handler previously had .Include(o => o.Items) without .ThenInclude(Product),
        // and MarkItemAsShipped dereferences item.Product.SKU - a NullReferenceException on every call. It
        // also never called SaveChangesAsync, so even a successful mark-as-shipped was silently discarded.
        (Guid orderId, int productId, string sku) = await SeedOrderWithSingleItemAsync();

        ProductShippedIntegrationEvent shippedEvent = new(sku, 1, orderId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderSubmittedOrchestration>().ConsumeAsync(shippedEvent, CancellationToken.None));

        Order? order = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking()
                .Include(o => o.Items)
                .SingleOrDefaultAsync(o => o.Id == orderId));

        Assert.NotNull(order);
        Assert.True(order.Items.Single(i => i.ProductId == productId).ShippedStatus.IsShipped);
    }

    [Fact]
    public async Task ConsumeAsync_OrderShippedEvent_PublishesOrderShippedIntegrationEventWithProducts()
    {
        // Regression test: this handler previously read order.Items with no .Include at all, so the
        // published OrderShippedIntegrationEvent.Products was always an empty array.
        (Guid orderId, _, string sku) = await SeedOrderWithSingleItemAsync();

        ProductShippedIntegrationEvent shippedEvent = new(sku, 1, orderId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderSubmittedOrchestration>().ConsumeAsync(shippedEvent, CancellationToken.None));

        Guid customerId = await OrderTestHelpers.RunAsync(_fixture, async sp =>
            (await sp.GetRequiredService<OrderDbContext>().Orders.AsNoTracking().SingleAsync(o => o.Id == orderId)).CustomerId);

        OrderShippedEvent orderShippedEvent = new(orderId, customerId, Price.Create(9.99m));

        await OrderTestHelpers.RunAsync(_fixture, async sp =>
            await sp.GetRequiredService<OrderSubmittedOrchestration>().ConsumeAsync(orderShippedEvent, CancellationToken.None));

        FakeIntegrationEventPublisher publisher = _fixture.Services.GetRequiredService<FakeIntegrationEventPublisher>();
        OrderShippedIntegrationEvent? published = publisher.Published<OrderShippedIntegrationEvent>().SingleOrDefault(e => e.OrderId == orderId);

        Assert.NotNull(published);
        Assert.Single(published.Products);
    }
}
