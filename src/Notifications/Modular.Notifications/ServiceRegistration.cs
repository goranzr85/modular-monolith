using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddScoped<OrderShippedNotificationHandler>();
        services.AddScoped<CustomerEventsHandler>();
        return services;
    }

    public static void ReceiveNotificationsEndpoints(this IRabbitMqBusFactoryConfigurator configurator, IBusRegistrationContext context)
    {
        configurator.ReceiveEndpoint("notification-customer-queue", e =>
        {
            e.ConfigureConsumer<CustomerEventsHandler>(context);
        });

        configurator.ReceiveEndpoint("notification-order-shipped-queue", e =>
        {
            e.ConfigureConsumer<OrderShippedNotificationHandler>(context);
        });
    }

    public static void AddNotificationConsumers(this IBusRegistrationConfigurator brc)
    {
        brc.AddConsumer<CustomerEventsHandler>();
        brc.AddConsumer<OrderShippedNotificationHandler>();
    }
}
