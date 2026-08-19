using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modular.Common.Events;
using Modular.Common.Messaging;
using Modular.Customers.Infrastructure;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
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
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eshop")
        .Build();

    private readonly RabbitMqContainer _rabbitContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:4-management-alpine")
        .Build();

    private ServiceProvider? _serviceProvider;
    private IConnection? _connection;

    public ServiceProvider Services => _serviceProvider
        ?? throw new InvalidOperationException($"{nameof(CustomerDatabaseFixture)} has not been initialized yet.");

    public IConnection Connection => _connection
        ?? throw new InvalidOperationException($"{nameof(CustomerDatabaseFixture)} has not been initialized yet.");

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgresContainer.StartAsync(), _rabbitContainer.StartAsync());

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:eshop"] = _postgresContainer.GetConnectionString()
            })
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IHostApplicationLifetime, NoopHostApplicationLifetime>();
        services.RegisterCustomerModule(configuration);
        services.RegisterCustomersBackgroundJobs();

        ConnectionFactory factory = new() { Uri = new Uri(_rabbitContainer.GetConnectionString()) };
        _connection = await factory.CreateConnectionAsync();
        services.AddSingleton(_connection);
        services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();

        _serviceProvider = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CustomerDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await _rabbitContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
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
