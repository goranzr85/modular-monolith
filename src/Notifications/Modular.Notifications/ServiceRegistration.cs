using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common.Messaging;
using Modular.Notifications.Customers;
using Modular.Notifications.Orders;

namespace Modular.Notifications;
public static class ServiceRegistration
{
    public static IServiceCollection RegisterNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("eshop");

        services.AddDbContext<NotificationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                // Specify the schema and table name for the migration history
                o.MigrationsHistoryTable("__EFMigrationsHistory", NotificationDbContext.Schema);
                o.MigrationsAssembly(typeof(NotificationDbContext).Assembly.FullName);
            });
        });

        return services;
    }

    public static IServiceCollection AddNotificationConsumers(this IServiceCollection services)
    {
        services.AddScoped<CustomerEventsHandler>();
        services.AddScoped<OrderShippedNotificationHandler>();

        services.AddHostedService<RabbitMqConsumerHostedService<CustomerEventsHandler>>();
        services.AddHostedService<RabbitMqConsumerHostedService<OrderShippedNotificationHandler>>();

        return services;
    }
}
