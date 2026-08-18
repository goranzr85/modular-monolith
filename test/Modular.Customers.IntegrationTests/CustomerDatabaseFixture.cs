using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modular.Common.Behaviors;
using Testcontainers.PostgreSql;
using Xunit;

namespace Modular.Customers.IntegrationTests;

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
        services.RegisterCustomerModule(configuration);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

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

        await _container.DisposeAsync();
    }
}

[CollectionDefinition(nameof(CustomerDatabaseCollection))]
public sealed class CustomerDatabaseCollection : ICollectionFixture<CustomerDatabaseFixture>
{
}
