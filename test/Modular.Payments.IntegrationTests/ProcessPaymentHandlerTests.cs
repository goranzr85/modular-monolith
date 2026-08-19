using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Orders.Integrations;
using Modular.Payments.IntegrationEvents;
using Xunit;

namespace Modular.Payments.IntegrationTests;

public sealed class ProcessPaymentHandlerTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<ProcessPaymentHandler>();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task Consume_WithProcessPayment_PublishesPaymentProcessedForSameOrder()
    {
        Guid orderId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();

        await _harness.Bus.Publish(new ProcessPayment(orderId, customerId, Price.Create(49.99m)));

        Assert.True(await _harness.Consumed.Any<ProcessPayment>());
        Assert.True(await _harness.Published.Any<PaymentProcessedIntegrationEvent>(e => e.Context.Message.OrderId == orderId));
    }

    [Fact]
    public async Task Consume_WithZeroTotalAmount_StillPublishesPaymentProcessed()
    {
        // Documents current behavior: ProcessPaymentHandler has no payment logic at all (see the
        // "process payment logic here" comment) - it unconditionally publishes success regardless of
        // amount, customer, or any real payment outcome.
        Guid orderId = Guid.NewGuid();

        await _harness.Bus.Publish(new ProcessPayment(orderId, Guid.NewGuid(), Price.Create(0m)));

        Assert.True(await _harness.Published.Any<PaymentProcessedIntegrationEvent>(e => e.Context.Message.OrderId == orderId));
    }

    [Fact]
    public async Task Consume_WithDifferentOrders_PublishesOneEventPerOrder()
    {
        Guid firstOrderId = Guid.NewGuid();
        Guid secondOrderId = Guid.NewGuid();

        await _harness.Bus.Publish(new ProcessPayment(firstOrderId, Guid.NewGuid(), Price.Create(10m)));
        await _harness.Bus.Publish(new ProcessPayment(secondOrderId, Guid.NewGuid(), Price.Create(20m)));

        Assert.True(await _harness.Published.Any<PaymentProcessedIntegrationEvent>(e => e.Context.Message.OrderId == firstOrderId));
        Assert.True(await _harness.Published.Any<PaymentProcessedIntegrationEvent>(e => e.Context.Message.OrderId == secondOrderId));
    }
}
