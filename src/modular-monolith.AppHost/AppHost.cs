var builder = DistributedApplication.CreateBuilder(args);

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
    .WithPgAdmin();

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
    .WithManagementPlugin();

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume()
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Modular_WebApi>("modular-webapi")
    .WithReference(postgresDb)
    .WaitFor(postgresDb)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithReference(keycloak)
    .WaitFor(keycloak);

await builder.Build().RunAsync();
