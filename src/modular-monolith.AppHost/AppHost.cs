
var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithBindMount(
        "./keycloak-config/eshop-realm-export.json",
        "/opt/keycloak/data/import/eshop-realm-export.json"
    )
    .WithArgs("--import-realm")
    .WithDataVolume()
    .WithExternalHttpEndpoints()
    .WithLifetime(ContainerLifetime.Persistent);

var username = builder.AddParameter(
    name: "postgres-username",
    value: builder.Configuration["postgres-username"]!,
    secret: true);

var password = builder.AddParameter(
    name: "postgres-password",
    value: builder.Configuration["postgres-password"]!,
    secret: true);

var postgres = builder
    .AddPostgres("postgres-db", username, password)
    .WithPgAdmin(c => c.WithLifetime(ContainerLifetime.Persistent))
    .WithLifetime(ContainerLifetime.Persistent);

var postgresDb = postgres
    .WithDataVolume()
    .AddDatabase("eshop");

var rabbitMqUsername = builder.AddParameter(
    name: "rabbitmq-username",
    value: builder.Configuration["rabbitmq-username"]!,
    secret: true);

var rabbitMqPassword = builder.AddParameter(
    name: "rabbitmq-password",
    value: builder.Configuration["rabbitmq-password"]!,
    secret: true);

var rabbitmq = builder.AddRabbitMQ("rabbitmq", rabbitMqUsername, rabbitMqPassword)
    .WithDataVolume()
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.Modular_WebApi>("modular-webapi")
    .WithReference(postgresDb)
    .WaitFor(postgresDb)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithReference(keycloak)
    .WaitFor(keycloak);

await builder.Build().RunAsync();
