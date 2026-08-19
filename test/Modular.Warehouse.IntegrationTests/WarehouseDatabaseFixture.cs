using JasperFx;
using JasperFx.Events;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modular.Common.Events;
using Modular.Common.Messaging;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Modular.Warehouse.IntegrationTests;

public sealed class WarehouseTestApp : IAsyncDisposable
{
    public required ServiceProvider Services { get; init; }
    public required IIntegrationEventPublisher Publisher { get; init; }
    public required IConnection Connection { get; init; }
    public required IReadOnlyList<IHostedService> HostedServices { get; init; }

    public async ValueTask DisposeAsync()
    {
        foreach (IHostedService hostedService in HostedServices)
        {
            await hostedService.StopAsync(CancellationToken.None);
        }

        await Connection.DisposeAsync();
        await Services.DisposeAsync();
    }
}

// Only the (expensive-to-start) containers are shared across a collection; each test builds and starts
// its own DI container + RabbitMQ connection against that broker (see the Notifications module tests for
// why: sharing one long-lived app across many tests proved flaky).
public sealed class WarehouseDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eshop")
        .Build();

    private readonly RabbitMqContainer _rabbitContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:4-management-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgresContainer.StartAsync(), _rabbitContainer.StartAsync());

        await using WarehouseTestApp app = await CreateAppAsync();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrderDbContext>().Database.MigrateAsync();
    }

    public async Task<WarehouseTestApp> CreateAppAsync()
    {
        string connectionString = _postgresContainer.GetConnectionString();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:eshop"] = connectionString
            })
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddWarehouse(configuration);

        services.AddMarten(_ =>
        {
            StoreOptions opts = new();
            opts.Connection(connectionString);
            opts.AutoCreateSchemaObjects = AutoCreate.All;
            opts.UseSystemTextJsonForSerialization();
            opts.Events.StreamIdentity = StreamIdentity.AsString;
            opts.DatabaseSchemaName = "Warehouse";
            opts.Events.DatabaseSchemaName = "Warehouse";
            opts.Projections.Snapshot<Modular.Warehouse.UseCases.Products.Models.Product>(JasperFx.Events.Projections.SnapshotLifecycle.Inline);

            return opts;
        });

        ConnectionFactory factory = new() { Uri = new Uri(_rabbitContainer.GetConnectionString()) };
        IConnection connection = await factory.CreateConnectionAsync();
        services.AddSingleton(connection);
        services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();

        services.AddScoped<Modular.Warehouse.UseCases.Products.Create.ProductCreatedNotificationHandler>();
        services.AddHostedService<RabbitMqConsumerHostedService<Modular.Warehouse.UseCases.Products.Create.ProductCreatedNotificationHandler>>();

        ServiceProvider provider = services.BuildServiceProvider();

        List<IHostedService> hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (IHostedService hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        return new WarehouseTestApp
        {
            Services = provider,
            Publisher = provider.GetRequiredService<IIntegrationEventPublisher>(),
            Connection = connection,
            HostedServices = hostedServices
        };
    }

    public async Task DisposeAsync()
    {
        await _rabbitContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}

[CollectionDefinition(nameof(WarehouseDatabaseCollection))]
public sealed class WarehouseDatabaseCollection : ICollectionFixture<WarehouseDatabaseFixture>
{
}
