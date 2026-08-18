using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modular.Orders.UseCases.Orders.Cancel;
using Modular.Orders.UseCases.Orders.Change.AddProducts;
using Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Decrease;
using Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Increase;
using Modular.Orders.UseCases.Orders.Change.RemoveProducts;
using Modular.Orders.UseCases.Orders.Create;
using Modular.Orders.UseCases.Orders.Submitted;
using Modular.Orders.UseCases.Products.Created;
using Modular.Orders.UseCases.Products.Received;

namespace Modular.Orders;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterOrderModule(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("eshop");

        services.AddDbContext<OrderDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                // Specify the schema and table name for the migration history
                o.MigrationsHistoryTable("__EFMigrationsHistory", OrderDbContext.Schema);
                o.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<CreateOrderCommandHandler>();
        services.AddScoped<OrderSubmitCommandHandler>();
        services.AddScoped<CancelOrderCommandHandler>();
        services.AddScoped<AddProductCommandHandler>();
        services.AddScoped<RemoveProductCommandHandler>();
        services.AddScoped<IncreaseProductQuantityCommandHandler>();
        services.AddScoped<DecreaseProductQuantityCommandHandler>();

        services.AddScoped<ProductCreatedEventHandler>();

        return services;
    }

    public static void ReceiveOrderEndpoints(this IRabbitMqBusFactoryConfigurator configurator, IBusRegistrationContext context)
    {
        configurator.ReceiveEndpoint("order-product-created-queue", e =>
        {
            e.ConfigureConsumer<ProductCreatedEventHandler>(context);
        });

        configurator.ReceiveEndpoint("order-product-received-queue", e =>
        {
            e.ConfigureConsumer<ProductReceivedEventHandler>(context);
        });
    }

    public static void AddOrderConsumers(this IBusRegistrationConfigurator brc)
    {
        brc.AddConsumer<ProductCreatedEventHandler>();
        brc.AddConsumer<ProductReceivedEventHandler>();
    }
}
