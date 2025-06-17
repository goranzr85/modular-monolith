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
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

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
}
