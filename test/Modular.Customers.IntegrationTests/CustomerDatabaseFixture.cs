using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modular.Customers.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace Modular.Customers.IntegrationTests;

internal sealed class NoopHostApplicationLifetime : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => CancellationToken.None;
    public CancellationToken ApplicationStopped => CancellationToken.None;
    public void StopApplication()
    {
    }
}

public sealed class CustomerDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eshop")
        .Build();

    private ServiceProvider? _serviceProvider;

    public ServiceProvider Services => _serviceProvider
        ?? throw new InvalidOperationException($"{nameof(CustomerDatabaseFixture)} has not been initialized yet.");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:eshop"] = _container.GetConnectionString()
            })
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IHostApplicationLifetime, NoopHostApplicationLifetime>();
        services.RegisterCustomerModule(configuration);
        services.RegisterCustomersBackgroundJobs();
        services.AddMassTransitTestHarness();

        _serviceProvider = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CustomerDbContext>().Database.MigrateAsync();

        await _serviceProvider.GetRequiredService<ITestHarness>().Start();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.GetRequiredService<ITestHarness>().Stop();
            await _serviceProvider.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}

[CollectionDefinition(nameof(CustomerDatabaseCollection))]
public sealed class CustomerDatabaseCollection : ICollectionFixture<CustomerDatabaseFixture>
{
}

// Outbox job tests read "all pending outbox messages" (Take(20), no ordering guarantee), so they need
// a database that isn't accumulating unrelated pending rows from the CRUD tests above - hence a separate
// collection with its own CustomerDatabaseFixture instance rather than sharing CustomerDatabaseCollection.
[CollectionDefinition(nameof(OutboxJobDatabaseCollection))]
public sealed class OutboxJobDatabaseCollection : ICollectionFixture<CustomerDatabaseFixture>
{
}
