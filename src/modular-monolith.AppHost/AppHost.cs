var builder = DistributedApplication.CreateBuilder(args);

// Pomoćna funkcija koja konvertuje Windows C:\ putanje u WSL /mnt/c/ format
static string ToWslPath(string path)
{
    var fullPath = Path.GetFullPath(path);
    if (fullPath.StartsWith("/mnt/") || fullPath.StartsWith("/"))
    {
        return fullPath;
    }

    var driveLetter = char.ToLowerInvariant(fullPath[0]);
    var relativePath = fullPath.Substring(3).Replace('\\', '/');
    return $"/mnt/{driveLetter}/{relativePath}";
}

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithBindMount(
        ToWslPath("./keycloak-config/eshop-realm-export.json"),
        "/opt/keycloak/data/import/eshop-realm-export.json"
    )
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