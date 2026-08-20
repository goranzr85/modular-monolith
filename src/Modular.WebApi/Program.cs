using Carter;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Modular.Authorization;
using Modular.Catalog;
using Modular.Catalog.Infrastructure;
using Modular.Common.Events;
using Modular.Common.Messaging;
using Modular.Customers;
using Modular.Customers.Infrastructure;
using Modular.Notifications;
using Modular.Notifications.Infrastructure;
using Modular.Orders;
using Modular.Payments;
using Modular.Warehouse;
using Modular.WebApi;
using Modular.WebApi.MIddlewares;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSwagger(builder.Configuration);

builder.Services
    .RegisterCustomerModule(builder.Configuration)
    .RegisterCustomersBackgroundJobs();
builder.Services
    .RegisterCatalogModule(builder.Configuration)
    .RegisterCatalogsBackgroundJobs();

builder.Services
    .RegisterNotificationsModule(builder.Configuration)
    .RegisterNotificationsBackgroundJobs();

builder.Services.RegisterOrderModule(builder.Configuration);

builder.Services.AddWarehouse(builder.Configuration);

builder.Services.AddCarter();
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", realm: "eshop-realm", options =>
{
    options.RequireHttpsMetadata = false;
    options.Audience = "account";
});

builder.AddRabbitMQClient("rabbitmq");

builder.Services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();

builder.Services.AddOrderConsumers();
builder.Services.AddWarehouseConsumers(builder.Configuration);
builder.Services.AddNotificationConsumers();
builder.Services
    .RegisterPaymentsModule(builder.Configuration)
    .AddPaymentsConsumers();

builder.Services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

builder.Services.AddAuthorization();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    await sp.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<Modular.Orders.OrderDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<Modular.Warehouse.OrderDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<CustomerDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<NotificationDbContext>().Database.MigrateAsync();
}

app.MapDefaultEndpoints();

app.UseAuthentication();

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(builder.Configuration);
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapCarter();

await app.RunAsync();
