using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using OrderConstants = Modular.Orders.UseCases.Common.Constants;
using CustomerConstants = Modular.Customers.Constants;
using WarehouseConstants = Modular.Warehouse.Constants;
using CatalogConstants = Modular.Catalog.Constants;

namespace Modular.WebApi;

public static class SwaggerConfiguration
{
    private const string customerDocName = "customers";
    private const string customerDocNameTitle = "Modular E-Shop - Customers API";
    private const string orderDocName = "orders";
    private const string orderDocNameTitle = "Modular E-Shop - Orders API";
    private const string catalogsDocName = "catalogs";
    private const string catalogsDocNameTitle = "Modular E-Shop - Catalogs API";
    private const string warehouseDocName = "warehouse";
    private const string warehouseDocNameTitle = "Modular E-Shop - Warehouse API";
    private static readonly Dictionary<string, string> scopes = new()
    {
        { "openid", "OpenID Connect scope" },
        { "profile", "User profile" },
        { "email", "User email" }
    };

    public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(o =>
        {
            o.CustomSchemaIds(type => type.FullName!.Replace("+", "."));

            o.SwaggerDoc(customerDocName, new OpenApiInfo
            {
                Title = customerDocNameTitle,
                Version = "v1"
            });

            o.SwaggerDoc(orderDocName, new OpenApiInfo
            {
                Title = orderDocNameTitle,
                Version = "v1"
            });

            o.SwaggerDoc(warehouseDocName, new OpenApiInfo
            {
                Title = warehouseDocNameTitle,
                Version = "v1"
            });

            o.SwaggerDoc(catalogsDocName, new OpenApiInfo
            {
                Title = catalogsDocNameTitle,
                Version = "v1"
            });

            o.DocInclusionPredicate((docName, apiDesc) =>
            {
                IEnumerable<string> tags = apiDesc.ActionDescriptor.EndpointMetadata
                                  .OfType<TagsAttribute>()
                                  .SelectMany(t => t.Tags);

                return docName switch
                {
                    catalogsDocName => tags.Any(t => t == CatalogConstants.EndpointTag),
                    warehouseDocName => tags.Any(t => t == WarehouseConstants.EndpointTag),
                    customerDocName => tags.Any(t => t == CustomerConstants.EndpointTag),
                    orderDocName => tags.Any(t => t == OrderConstants.EndpointTag),
                    _ => false
                };
            });

            string keycloakUrl = $"{configuration["Keycloak:Authority"]}/realms/eshop-realm";

            var securityScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{keycloakUrl}/protocol/openid-connect/auth"),
                        TokenUrl = new Uri($"{keycloakUrl}/protocol/openid-connect/token"),
                        Scopes = scopes
                    }
                },
                In = ParameterLocation.Header,
                Name = "Authorization",
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT",
                Description = "OAuth2 AuthorizationCode flow with Keycloak"
            };

            o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    },
                    scopes.Keys.ToList()
                }
            };

            o.AddSecurityRequirement(securityRequirement);
        });

        return services;
    }

    public static IApplicationBuilder UseSwagger(this IApplicationBuilder app, IConfiguration configuration)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.OAuthClientId(configuration["Keycloak:ClientId"]);
            options.OAuthUsePkce();
            options.OAuthScopes(scopes.Keys.ToArray());

            options.SwaggerEndpoint(
                "/swagger/customers/swagger.json",
                "Customers API v1");

            options.SwaggerEndpoint(
                "/swagger/orders/swagger.json",
                "Orders API v1");

            options.SwaggerEndpoint(
                "/swagger/catalogs/swagger.json",
                "Catalogs API v1");

            options.SwaggerEndpoint(
                "/swagger/warehouse/swagger.json",
                "Warehouse API v1");

            // Optional: Set a default route
            options.RoutePrefix = "swagger";
        });

        return app;
    }
}
