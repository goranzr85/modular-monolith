using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modular.Common.Events;
using Modular.Common.Messaging;
using Modular.Notifications.Infrastructure.NotificationSenders;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Modular.Notifications.IntegrationTests;

public sealed class NotificationTestApp : IAsyncDisposable
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

// Only the (expensive-to-start) containers are shared across a collection. Each test builds and starts
// its own DI container + RabbitMQ connection: sharing one long-lived app across many publish-driven tests
// proved flaky (consumer dispatch doesn't cleanly isolate between unrelated tests sharing a connection).
public sealed class NotificationDatabaseFixture : IAsyncLifetime
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

        await using NotificationTestApp app = await CreateAppAsync();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.MigrateAsync();
    }

    public async Task<NotificationTestApp> CreateAppAsync()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:eshop"] = _postgresContainer.GetConnectionString()
            })
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.RegisterNotificationsModule(configuration);

        // Registered directly (rather than via RegisterNotificationsBackgroundJobs) to avoid pulling in
        // Quartz: Quartz.NET's logging bridge binds to the first ILoggerFactory it sees process-wide and
        // doesn't rebind per DI container, so a later test's container touching Quartz-registered services
        // throws ObjectDisposedException once an earlier test's container has been disposed.
        // ProcessInboxMessagesJob is invoked directly in tests, not through the Quartz scheduler.
        services.AddKeyedScoped<INotificationSender, EmailNotificationsSender>(EmailNotificationsSender.Key);
        services.AddKeyedScoped<INotificationSender, SmsNotificationsSender>(SmsNotificationsSender.Key);
        services.AddScoped<INotificationSender, NotificationSenderFactory>();

        ConnectionFactory factory = new() { Uri = new Uri(_rabbitContainer.GetConnectionString()) };
        IConnection connection = await factory.CreateConnectionAsync();
        services.AddSingleton(connection);
        services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();

        services.AddNotificationConsumers();

        ServiceProvider provider = services.BuildServiceProvider();

        List<IHostedService> hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (IHostedService hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        return new NotificationTestApp
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

[CollectionDefinition(nameof(NotificationDatabaseCollection))]
public sealed class NotificationDatabaseCollection : ICollectionFixture<NotificationDatabaseFixture>
{
}
