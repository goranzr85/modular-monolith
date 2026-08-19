using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modular.Catalog.UseCases.Change;
using Modular.Catalog.UseCases.Create;
using Modular.Common;

namespace Modular.Catalog;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("eshop");

        services.AddSingleton<EventsToOutboxMessagesInterceptors>();
        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<EventsToOutboxMessagesInterceptors>();

            options.UseNpgsql(connectionString, o =>
            {
                // Specify the schema and table name for the migration history
                o.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.Schema);
                o.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName);
            })
            .AddInterceptors(interceptor);
        });

        services.AddValidatorsFromAssembly(typeof(ServiceRegistration).Assembly, includeInternalTypes: true);

        services.AddScoped<CreateProductCommandHandler>();
        services.AddScoped<ChangeProductCommandHandler>();

        return services;
    }
}
