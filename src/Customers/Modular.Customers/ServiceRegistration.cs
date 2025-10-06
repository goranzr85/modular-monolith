using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Customers.Models;

namespace Modular.Customers;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("eshop");

        services.AddSingleton<EventsToOutboxMessagesInterceptors>();
        services.AddDbContext<CustomerDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<EventsToOutboxMessagesInterceptors>();

            options.UseNpgsql(connectionString, o =>
            {
                // Specify the schema and table name for the migration history
                o.MigrationsHistoryTable("__EFMigrationsHistory", CustomerDbContext.Schema);
                o.MigrationsAssembly(typeof(CustomerDbContext).Assembly.FullName);
            })
            .AddInterceptors(interceptor);
        });


        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ServiceRegistration).Assembly));

        services.AddScoped<ContactFactory>();

        return services;
    }
}
