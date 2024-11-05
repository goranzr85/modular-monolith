using Microsoft.EntityFrameworkCore;

namespace Modular.Catalog;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterCustomerModule(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<CatalogDbContext>(options =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                // Specify the schema and table name for the migration history
                o.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.Schema);
                o.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName);
            });
        });

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ServiceRegistration).Assembly));

        return services;
    }
}
