using Microsoft.EntityFrameworkCore;

namespace Modular.Catalog;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterCustomerModule(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddSingleton<EventsDoOutboXMessagesInterceptors>();
        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<EventsDoOutboXMessagesInterceptors>();

            options.UseNpgsql(connectionString, o =>
            {
                // Specify the schema and table name for the migration history
                o.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.Schema);
                o.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName);
            })
            .AddInterceptors(interceptor);
        });

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ServiceRegistration).Assembly));

        return services;
    }
}
