using JasperFx;
using JasperFx.Events;
using MassTransit;
using MassTransit.Testing;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace Modular.Warehouse.IntegrationTests;

public sealed class WarehouseTestApp : IAsyncDisposable
{
    public required ServiceProvider Services { get; init; }
    public required ITestHarness Harness { get; init; }

    public async ValueTask DisposeAsync()
    {
        await Harness.Stop();
        await Services.DisposeAsync();
    }
}

// Only the (expensive-to-start) Postgres container is shared across a collection; each test builds and
// starts its own DI container + MassTransit test harness against that database (see the Notifications
// module tests for why: sharing one long-lived harness across many tests proved flaky).
public sealed class WarehouseDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eshop")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using WarehouseTestApp app = await CreateAppAsync();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrderDbContext>().Database.MigrateAsync();
    }

    public async Task<WarehouseTestApp> CreateAppAsync()
    {
        string connectionString = _container.GetConnectionString();

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

        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddConsumer<Modular.Warehouse.UseCases.Products.Create.ProductCreatedNotificationHandler>();
        });

        ServiceProvider provider = services.BuildServiceProvider();
        ITestHarness harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        return new WarehouseTestApp { Services = provider, Harness = harness };
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(nameof(WarehouseDatabaseCollection))]
public sealed class WarehouseDatabaseCollection : ICollectionFixture<WarehouseDatabaseFixture>
{
}
