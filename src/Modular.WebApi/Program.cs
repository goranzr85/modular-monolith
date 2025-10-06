using Carter;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Modular.Catalog;
using Modular.Catalog.Infrastructure;
using Modular.Customers;
using Modular.Notifications;
using Modular.Notifications.Infrastructure;
using Modular.Orders;
using Modular.Warehouse;
using Modular.WebApi.MIddlewares;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.CustomSchemaIds(type => type.FullName!.Replace("+", "."));

    var keycloakUrl = $"{builder.Configuration["Keycloak:Authority"]}/realms/eshop";
    //var clientId = builder.Configuration["Keycloak:SwaggerClientId"] ?? "swagger-ui";
    var scopes = new Dictionary<string, string>
    {
        { "openid", "OpenID Connect scope" },
        { "profile", "User profile" },
        { "email", "User email" }
    };

    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.OAuth2,
        Flows = new Microsoft.OpenApi.Models.OpenApiOAuthFlows
        {
            AuthorizationCode = new Microsoft.OpenApi.Models.OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{keycloakUrl}/protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{keycloakUrl}/protocol/openid-connect/token"),
                Scopes = scopes
            }
        },
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "Authorization",
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        Description = "OAuth2 AuthorizationCode flow with Keycloak"
    };

    o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);

    var securityRequirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            new[] { "openid", "profile", "email" }
        }
    };

    o.AddSecurityRequirement(securityRequirement);
});
builder.Services.RegisterCustomerModule(builder.Configuration);
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
    .AddKeycloakJwtBearer("keycloak", realm: "eshop", options =>
{
    options.RequireHttpsMetadata = false;
    options.Audience = "account";
});

builder.AddMassTransitRabbitMq("rabbitmq", options =>
{
    options.DisableTelemetry = false;
},
    consumers =>
    {
        consumers.SetKebabCaseEndpointNameFormatter();
        consumers.AddOrderConsumers();
        consumers.AddWarehouseConsumers(builder.Configuration);
        consumers.AddNotificationConsumers();

        consumers.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            cfg.UseMessageRetry(r => r.Immediate(5));
        });
    }
);

builder.Services.AddAuthorization();

//builder.AddRabbitMQClient("rabbitmq");

//builder.Services.AddMassTransit(mt =>
//{
//    mt.SetKebabCaseEndpointNameFormatter();
//    mt.AddOrderConsumers();
//    mt.AddWarehouseConsumers();
//    mt.AddNotificationConsumers();

//    mt.AddConfigureEndpointsCallback((context, name, cfg) =>
//    {
//        cfg.UseMessageRetry(r => r.Immediate(5));
//    });

//    mt.UsingRabbitMq((context, cfg) =>
//    {
//        var rabbit = context.GetRequiredService<RabbitMQConnection>();

//        cfg.Host(rabbit.Host, rabbit.Port, "/", h =>
//        {
//            h.Username(rabbit.UserName);
//            h.Password(rabbit.Password);
//        });

//        cfg.ReceiveOrderEndpoints(context);
//        cfg.WarehouseEndpoints(context);
//        cfg.ReceiveNotificationsEndpoints(context);
//    });

//    //mt.UsingRabbitMq((context, cfg) =>
//    //{
//    //    cfg.Host(new Uri("rabbitmq://localhost:5673"), h =>
//    //    {
//    //        h.Username("admin");
//    //        h.Password("admin");
//    //    });

//    //    cfg.ReceiveOrderEndpoints(context);
//    //    cfg.WarehouseEndpoints(context);
//    //    cfg.ReceiveNotificationsEndpoints(context);
//    //});
//});

//var serviceProvider = builder.Services.BuildServiceProvider();

//await serviceProvider.GetRequiredService<CatalogDbContext>()
//    .Database
//    .MigrateAsync();

// Remove the following lines that call BuildServiceProvider and perform migrations directly
// var serviceProvider = builder.Services.BuildServiceProvider();
//
// await serviceProvider.GetRequiredService<CatalogDbContext>()
//     .Database
//     .MigrateAsync();
//
// await serviceProvider.GetRequiredService<Modular.Orders.OrderDbContext>()
//     .Database
//     .MigrateAsync();
//
// serviceProvider.GetRequiredService<Modular.Warehouse.OrderDbContext>()
//     .Database
//     .Migrate();
//
// serviceProvider.GetRequiredService<CustomerDbContext>()
//     .Database
//     .Migrate();
//
// serviceProvider.GetRequiredService<NotificationDbContext>()
//     .Database
//     .Migrate();

// Instead, perform migrations after building the app, using app.Services (the root service provider)
var app = builder.Build();

// Apply migrations using app.Services to avoid ASP0000
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    await sp.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<Modular.Orders.OrderDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<Modular.Warehouse.OrderDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<CustomerDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<NotificationDbContext>().Database.MigrateAsync();
}

//var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapCarter();

await app.RunAsync();
