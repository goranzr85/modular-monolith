using Marten;
using Marten.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modular.Warehouse.Create;
using Weasel.Core;

namespace Modular.Warehouse;
public static class ServiceRegistration
{
    public static IServiceCollection AddWarehouse(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ServiceRegistration).Assembly));

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
            cfg.Events.DatabaseSchemaName = "Warehouse";
        });
    }
}
