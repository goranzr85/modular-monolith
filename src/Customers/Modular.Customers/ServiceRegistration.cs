using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modular.Authorization;
using Modular.Common;
using Modular.Customers.Models;
using Modular.Customers.UseCases.Change;
using Modular.Customers.UseCases.Create;

namespace Modular.Customers;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterCustomerModule(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("eshop");

        services.AddSingleton<EventsToOutboxMessagesInterceptors>();
        services.AddDbContext<CustomerDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<EventsToOutboxMessagesInterceptors>();

            options.UseNpgsql(connectionString, o =>
            {
                o.MigrationsHistoryTable("__EFMigrationsHistory", CustomerDbContext.Schema);
                o.MigrationsAssembly(typeof(CustomerDbContext).Assembly.FullName);
            })
            .AddInterceptors(interceptor);
        });


        services.AddValidatorsFromAssembly(typeof(ServiceRegistration).Assembly, includeInternalTypes: true);

        services.AddScoped<CreateCustomerCommandHandler>();
        services.AddScoped<ChangeCustomerCommandHandler>();

        services.AddScoped<ContactFactory>();

        return services;
    }
}
