using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common.Messaging;
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

        return services;
    }

    public static IServiceCollection AddOrderConsumers(this IServiceCollection services)
    {
        services.AddScoped<ProductCreatedEventHandler>();
        services.AddScoped<ProductReceivedEventHandler>();
        services.AddScoped<OrderCreatedEventHandler>();
        services.AddScoped<OrderCanceledEventHandler>();
        services.AddScoped<OrderItemAddedEventHandler>();
        services.AddScoped<OrderItemRemovedEventHandler>();
        services.AddScoped<OrderSubmittedOrchestration>();

        services.AddHostedService<RabbitMqConsumerHostedService<ProductCreatedEventHandler>>();
        services.AddHostedService<RabbitMqConsumerHostedService<ProductReceivedEventHandler>>();
        services.AddHostedService<RabbitMqConsumerHostedService<OrderCreatedEventHandler>>();
        services.AddHostedService<RabbitMqConsumerHostedService<OrderCanceledEventHandler>>();
        services.AddHostedService<RabbitMqConsumerHostedService<OrderItemAddedEventHandler>>();
        services.AddHostedService<RabbitMqConsumerHostedService<OrderItemRemovedEventHandler>>();
        services.AddHostedService<RabbitMqConsumerHostedService<OrderSubmittedOrchestration>>();

        return services;
    }
}
