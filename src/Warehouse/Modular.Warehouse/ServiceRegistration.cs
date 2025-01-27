using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Modular.Warehouse;
public static class ServiceRegistration
{
    public static IServiceCollection AddWarehouse(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<WarehouseDbContext>(options =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                // Specify the schema and table name for the migration history
                //o.MigrationsHistoryTable("__EFMigrationsHistory", CustomerDbContext.Schema);
                //o.MigrationsAssembly(typeof(CustomerDbContext).Assembly.FullName);
            });
        });

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ServiceRegistration).Assembly));

        return services;
    }
}
