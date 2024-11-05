using Microsoft.EntityFrameworkCore;
using Modular.Customers.Models;

namespace Modular.Customers;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<CustomerDbContext>(options =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                // Specify the schema and table name for the migration history
                o.MigrationsHistoryTable("__EFMigrationsHistory", CustomerDbContext.Schema);
                o.MigrationsAssembly(typeof(CustomerDbContext).Assembly.FullName);
            });
        });

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ServiceRegistration).Assembly));

        services.AddScoped<ContactFactory>();

        return services;
    }
}
