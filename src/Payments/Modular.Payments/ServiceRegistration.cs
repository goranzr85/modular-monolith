using Microsoft.Extensions.DependencyInjection;
using Modular.Common.Messaging;

namespace Modular.Payments;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentsConsumers(this IServiceCollection services)
    {
        services.AddScoped<ProcessPaymentHandler>();
        services.AddHostedService<RabbitMqConsumerHostedService<ProcessPaymentHandler>>();

        return services;
    }
}
