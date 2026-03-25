using Carter;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modular.Warehouse.UseCases.Products.Create;
using Weasel.Core;

namespace Modular.Warehouse;
public static class ServiceRegistration
{
    public static IServiceCollection AddWarehouse(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ServiceRegistration).Assembly));

        string? connectionString = configuration.GetConnectionString("eshop");

        services.AddDbContext<OrderDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                o.MigrationsHistoryTable("__EFMigrationsHistory", OrderDbContext.Schema);
                o.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IntegrationEventPublisher>();

        return services;
    }

    public static void WarehouseEndpoints(this IRabbitMqBusFactoryConfigurator configurator, IBusRegistrationContext context)
    {
        configurator.ReceiveEndpoint("warehouse-product-created-queue", e =>
        {
            e.ConfigureConsumer<ProductCreatedNotificationHandler>(context);
        });
    }

    public static void AddWarehouseConsumers(this IBusRegistrationConfigurator brc, IConfiguration configuration, IServiceCollection services)
    {
        //using IServiceScope scope = services.BuildServiceProvider().CreateScope();

        //IntegrationEventPublisher integrationEventPublisher = scope.ServiceProvider.GetService<IntegrationEventPublisher>();

        brc.AddConsumer<ProductCreatedNotificationHandler>();

        string? connectionString = configuration.GetConnectionString("eshop");

        services.AddMarten(sp =>
        {
            var opts = new StoreOptions();
            opts.Connection(connectionString!);
            opts.AutoCreateSchemaObjects = AutoCreate.All;
            opts.UseSystemTextJsonForSerialization();
            opts.Events.StreamIdentity = StreamIdentity.AsString;
            opts.DatabaseSchemaName = "Warehouse";
            opts.Events.DatabaseSchemaName = "Warehouse";

            var publisher = new IntegrationEventPublisher(sp);

            opts.Projections.Subscribe(publisher, projectionOpts =>
            {
                projectionOpts.SubscriptionName = "IntegrationEvents";
            });

            return opts;
        });

        //brc.AddMarten(cfg =>
        //{
        //    var opts = new StoreOptions();

        //    opts.Connection(connectionString!);
        //    opts.AutoCreateSchemaObjects = AutoCreate.All;
        //    opts.UseSystemTextJsonForSerialization();
        //    opts.Events.StreamIdentity = StreamIdentity.AsString;
        //    opts.DatabaseSchemaName = "Warehouse";
        //    opts.Events.DatabaseSchemaName = "Warehouse";

        //    var publisher = new IntegrationEventPublisher(sp);

        //    opts.Projections.Subscribe(publisher, );

        //    //cfg.Connection(connectionString!);
        //    //cfg.AutoCreateSchemaObjects = AutoCreate.All;
        //    //cfg.UseSystemTextJsonForSerialization();
        //    //cfg.Events.StreamIdentity = StreamIdentity.AsString;
        //    //cfg.DatabaseSchemaName = "Warehouse";
        //    //cfg.Events.DatabaseSchemaName = "Warehouse";
        //    //cfg.Projections.Subscribe(integrationEventPublisher,
        //    //    projectionOptions => projectionOptions.SubscriptionName = "IntegrationEvents");
        //}).AddAsyncDaemon(DaemonMode.Solo);
    }
}
