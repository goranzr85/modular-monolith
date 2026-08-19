using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modular.Notifications.Infrastructure.NotificationSenders;
using Testcontainers.PostgreSql;
using Xunit;

namespace Modular.Notifications.IntegrationTests;

public sealed class NotificationTestApp : IAsyncDisposable
{
    public required ServiceProvider Services { get; init; }
    public required ITestHarness Harness { get; init; }

    public async ValueTask DisposeAsync()
    {
        await Harness.Stop();
        await Services.DisposeAsync();
    }
}

// Only the (expensive-to-start) Postgres container is shared across a collection. Each test builds and
// starts its own DI container + MassTransit test harness against that database: sharing one long-lived
// harness across many Bus.Publish-driven tests proved flaky (Consumed/Published lists and consumer
// dispatch don't cleanly isolate between unrelated tests sharing a bus instance).
public sealed class NotificationDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eshop")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using NotificationTestApp app = await CreateAppAsync();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.MigrateAsync();
    }

    public async Task<NotificationTestApp> CreateAppAsync()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:eshop"] = _container.GetConnectionString()
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

        services.AddMassTransitTestHarness(cfg => cfg.AddNotificationConsumers());

        ServiceProvider provider = services.BuildServiceProvider();
        ITestHarness harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        return new NotificationTestApp { Services = provider, Harness = harness };
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(nameof(NotificationDatabaseCollection))]
public sealed class NotificationDatabaseCollection : ICollectionFixture<NotificationDatabaseFixture>
{
}
