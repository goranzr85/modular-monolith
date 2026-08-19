using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace Modular.Catalog.IntegrationTests;

public sealed class CatalogDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eshop")
        .Build();

    private ServiceProvider? _serviceProvider;

    public ServiceProvider Services => _serviceProvider
        ?? throw new InvalidOperationException($"{nameof(CatalogDatabaseFixture)} has not been initialized yet.");

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
        services.RegisterCatalogModule(configuration);

        _serviceProvider = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
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

[CollectionDefinition(nameof(CatalogDatabaseCollection))]
public sealed class CatalogDatabaseCollection : ICollectionFixture<CatalogDatabaseFixture>
{
}
