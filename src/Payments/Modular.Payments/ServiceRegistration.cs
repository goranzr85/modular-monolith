using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common.Messaging;
using Stripe;

namespace Modular.Payments;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterPaymentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        string? stripeSecretKey = configuration["Stripe:SecretKey"];

        services.AddSingleton<IStripeClient>(_ => new StripeClient(stripeSecretKey));
        services.AddSingleton(sp => new PaymentIntentService(sp.GetRequiredService<IStripeClient>()));
        services.AddScoped<IPaymentGatewayClient, StripePaymentGatewayClient>();

        return services;
    }

    public static IServiceCollection AddPaymentsConsumers(this IServiceCollection services)
    {
        services.AddScoped<ProcessPaymentHandler>();
        services.AddHostedService<RabbitMqConsumerHostedService<ProcessPaymentHandler>>();

        return services;
    }
}
