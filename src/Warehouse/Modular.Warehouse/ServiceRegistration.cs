using Carter;
using FluentValidation;
using JasperFx;
using JasperFx.Events;
using JasperFx.Events.Projections;
using Marten;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modular.Warehouse.UseCases.Products;
using Modular.Warehouse.UseCases.Products.Adjusted.Decreased;
using Modular.Warehouse.UseCases.Products.Adjusted.Increased;
using Modular.Warehouse.UseCases.Products.Create;
using Modular.Warehouse.UseCases.Products.Models;
using Modular.Warehouse.UseCases.Products.Receiving;
using Modular.Warehouse.UseCases.Products.Shipping;
using Weasel.Core;

namespace Modular.Warehouse;
public static class ServiceRegistration
{
    public static IServiceCollection AddWarehouse(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(typeof(ServiceRegistration).Assembly, includeInternalTypes: true);

        services.AddScoped<ProductReceivingCommandHandler>();
        services.AddScoped<ProductShippingCommandHandler>();
        services.AddScoped<ManualProductStockIncreaseCommandHandler>();
        services.AddScoped<ManualProductStockDecreaseCommandHandler>();

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
        services.AddScoped<IProductStreamStore, ProductStreamStore>();

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
                projectionOpts.Name = "IntegrationEvents";
            });

            opts.Projections.Snapshot<Product>(SnapshotLifecycle.Inline);

            return opts;
        });
    }
}
