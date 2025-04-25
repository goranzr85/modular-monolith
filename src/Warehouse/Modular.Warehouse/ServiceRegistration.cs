using Marten;
using Marten.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modular.Warehouse.UseCases.Products.Create;
using Weasel.Core;
using Microsoft.EntityFrameworkCore;

namespace Modular.Warehouse;
public static class ServiceRegistration
{
    public static IServiceCollection AddWarehouse(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ServiceRegistration).Assembly));

        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<OrderDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                o.MigrationsHistoryTable("__EFMigrationsHistory", OrderDbContext.Schema);
                o.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName);
            });
        });

        return services;
    }

    public static void WarehouseEndpoints(this IRabbitMqBusFactoryConfigurator configurator, IBusRegistrationContext context)
    {
        configurator.ReceiveEndpoint("warehouse-product-created-queue", e =>
        {
            e.ConfigureConsumer<ProductCreatedNotificationHandler>(context);
        });
    }

    public static void AddWarehouseConsumers(this IBusRegistrationConfigurator brc)
    {
        brc.AddConsumer<ProductCreatedNotificationHandler>();

        brc.AddMarten(cfg =>
        {
            cfg.Connection("Host=localhost;Database=mydatabase;Username=myuser;Password=mypassword");
            cfg.AutoCreateSchemaObjects = AutoCreate.All;
            cfg.UseSystemTextJsonForSerialization();
            cfg.Events.StreamIdentity = StreamIdentity.AsString;
            cfg.DatabaseSchemaName = "Warehouse";
            cfg.Events.DatabaseSchemaName = "Warehouse";
        });
    }
}
