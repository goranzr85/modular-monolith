# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["nuget.config", "."]
COPY ["src/Modular.WebApi/Modular.WebApi.csproj", "src/Modular.WebApi/"]
COPY ["src/Catalog/Modular.Catalog.Infrastructure/Modular.Catalog.Infrastructure.csproj", "src/Catalog/Modular.Catalog.Infrastructure/"]
COPY ["src/Common/Modular.Common/Modular.Common.csproj", "src/Common/Modular.Common/"]
COPY ["src/Catalog/Modular.Catalog/Modular.Catalog.csproj", "src/Catalog/Modular.Catalog/"]
COPY ["src/Catalog/Modular.ProductIntegrationEvents/Modular.Catalog.IntegrationEvents.csproj", "src/Catalog/Modular.ProductIntegrationEvents/"]
COPY ["src/Customers/Modular.Customers/Modular.Customers.csproj", "src/Customers/Modular.Customers/"]
COPY ["src/Customers/Modular.Customers.IntegrationEvents/Modular.Customers.IntegrationEvents.csproj", "src/Customers/Modular.Customers.IntegrationEvents/"]
COPY ["src/modular-monolith.ServiceDefaults/modular-monolith.ServiceDefaults.csproj", "src/modular-monolith.ServiceDefaults/"]
COPY ["src/Notifications/Modular.Notifications.Infrastructure/Modular.Notifications.Infrastructure.csproj", "src/Notifications/Modular.Notifications.Infrastructure/"]
COPY ["src/Notifications/Modular.Notifications/Modular.Notifications.csproj", "src/Notifications/Modular.Notifications/"]
COPY ["src/Orders/Modular.Orders.Infrastructure/Modular.Orders.Infrastructure.csproj", "src/Orders/Modular.Orders.Infrastructure/"]
COPY ["src/Orders/Modular.Orders/Modular.Orders.csproj", "src/Orders/Modular.Orders/"]
COPY ["src/Payments/Modular.Payments.IntegrationEvents/Modular.Payments.IntegrationEvents.csproj", "src/Payments/Modular.Payments.IntegrationEvents/"]
COPY ["src/Warehouse/Modular.Warehouse.IntegrationEvents/Modular.Warehouse.IntegrationEvents.csproj", "src/Warehouse/Modular.Warehouse.IntegrationEvents/"]
COPY ["src/Orders/Modular.Orders.IntegrationEvents/Modular.Orders.Integrations.csproj", "src/Orders/Modular.Orders.IntegrationEvents/"]
COPY ["src/Warehouse/Modular.Warehouse/Modular.Warehouse.csproj", "src/Warehouse/Modular.Warehouse/"]
RUN dotnet restore "./src/Modular.WebApi/Modular.WebApi.csproj"
COPY . .
WORKDIR "/src/src/Modular.WebApi"
RUN dotnet build "./Modular.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Modular.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Modular.WebApi.dll"]