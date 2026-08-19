using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Modular.Common;
using Modular.Common.Events;
using Modular.Common.Messaging;
using Modular.Orders.Integrations;
using Modular.Payments.IntegrationEvents;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace Modular.Payments.IntegrationTests;

public sealed class ProcessPaymentHandlerTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:4-management-alpine")
        .Build();

    private ServiceProvider _provider = null!;
    private IConnection _connection = null!;
    private IReadOnlyList<IHostedService> _hostedServices = null!;

    public async Task InitializeAsync()
    {
        await _rabbitContainer.StartAsync();

        ConnectionFactory factory = new() { Uri = new Uri(_rabbitContainer.GetConnectionString()) };
        _connection = await factory.CreateConnectionAsync();

        _provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(_connection)
            .AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>()
            .AddPaymentsConsumers()
            .BuildServiceProvider(true);

        _hostedServices = _provider.GetServices<IHostedService>().ToList();
        foreach (IHostedService hostedService in _hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }
    }

    public async Task DisposeAsync()
    {
        foreach (IHostedService hostedService in _hostedServices)
        {
            await hostedService.StopAsync(CancellationToken.None);
        }

        await _connection.DisposeAsync();
        await _provider.DisposeAsync();
        await _rabbitContainer.DisposeAsync();
    }

    [Fact]
    public async Task Consume_WithProcessPayment_PublishesPaymentProcessedForSameOrder()
    {
        Guid orderId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();

        await using PublishedMessageListener<PaymentProcessedIntegrationEvent> listener =
            await PublishedMessageListener<PaymentProcessedIntegrationEvent>.StartAsync(_connection);

        IIntegrationEventPublisher publisher = _provider.GetRequiredService<IIntegrationEventPublisher>();
        await publisher.PublishAsync(new ProcessPayment(orderId, customerId, Price.Create(49.99m)));

        Assert.True(await listener.AnyAsync(e => e.OrderId == orderId));
    }

    [Fact]
    public async Task Consume_WithZeroTotalAmount_StillPublishesPaymentProcessed()
    {
        // Documents current behavior: ProcessPaymentHandler has no payment logic at all (see the
        // "process payment logic here" comment) - it unconditionally publishes success regardless of
        // amount, customer, or any real payment outcome.
        Guid orderId = Guid.NewGuid();

        await using PublishedMessageListener<PaymentProcessedIntegrationEvent> listener =
            await PublishedMessageListener<PaymentProcessedIntegrationEvent>.StartAsync(_connection);

        IIntegrationEventPublisher publisher = _provider.GetRequiredService<IIntegrationEventPublisher>();
        await publisher.PublishAsync(new ProcessPayment(orderId, Guid.NewGuid(), Price.Create(0m)));

        Assert.True(await listener.AnyAsync(e => e.OrderId == orderId));
    }

    [Fact]
    public async Task Consume_WithDifferentOrders_PublishesOneEventPerOrder()
    {
        Guid firstOrderId = Guid.NewGuid();
        Guid secondOrderId = Guid.NewGuid();

        await using PublishedMessageListener<PaymentProcessedIntegrationEvent> listener =
            await PublishedMessageListener<PaymentProcessedIntegrationEvent>.StartAsync(_connection);

        IIntegrationEventPublisher publisher = _provider.GetRequiredService<IIntegrationEventPublisher>();
        await publisher.PublishAsync(new ProcessPayment(firstOrderId, Guid.NewGuid(), Price.Create(10m)));
        await publisher.PublishAsync(new ProcessPayment(secondOrderId, Guid.NewGuid(), Price.Create(20m)));

        Assert.True(await listener.AnyAsync(e => e.OrderId == firstOrderId));
        Assert.True(await listener.AnyAsync(e => e.OrderId == secondOrderId));
    }
}
