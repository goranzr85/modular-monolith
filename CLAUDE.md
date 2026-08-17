# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

A learning/reference project demonstrating a **modular monolith** architecture in .NET 8: asynchronous
inter-module communication via an outbox + message bus, event sourcing (in the Warehouse module), Aspire
for local orchestration, Keycloak-based RBAC, and Redis caching. See `README.md` for the full feature list
and the "still to be implemented" list (integration tests, CI/CD, architecture tests, OpenTelemetry,
structured logging, static analysis — none of these exist yet, so don't assume they do).

## Commands

- Build: `dotnet build modular-monolith.sln`
- Run the whole system (API + Postgres + RabbitMQ + Keycloak via Aspire): `dotnet run --project src/modular-monolith.AppHost`
- Run the API only (needs Postgres/RabbitMQ/Keycloak reachable via the connection strings/config it expects): `dotnet run --project src/Modular.WebApi`
- There are **no test projects in the solution yet** (the `.sln` has an empty `test` solution folder reserved for them). Don't invent `dotnet test` instructions or assume test coverage exists for a module until you've checked.
- EF Core migrations are per-module (see Persistence below), e.g. from a module's project directory:
  `dotnet ef migrations add <Name> --project src/Catalog/Modular.Catalog --startup-project src/Modular.WebApi`
- Package versions are centrally managed in `Directory.Packages.props` (`ManagePackageVersionsCentrally`) — add a `<PackageVersion>` there, then reference the package (no version) in the `.csproj`.
- Shared build settings (TargetFramework net8.0, Nullable/ImplicitUsings enabled, SonarAnalyzer) live in `Directory.Build.props` and apply to every project automatically.

## Architecture

### Module layout

Each business capability is a top-level folder under `src/` (Catalog, Customers, Orders, Payments,
Warehouse, Notifications), plus `Common` for cross-cutting code. Within a module you'll typically find
some subset of:

- `Modular.<Module>` — the core module: domain models, `UseCases/` (one folder per use case, MediatR
  command/handler + FluentValidation validator + Carter endpoint, colocated in the same file or folder),
  `Authorization/Permissions.cs`, `Errors/`, `Migrations/`, and the module's own `DbContext`.
- `Modular.<Module>.Infrastructure` — background jobs (Quartz) and the domain→integration event converter
  for that module. Not every module has one (Warehouse and Payments don't; Catalog, Orders, Notifications do).
- `Modular.<Module>.IntegrationEvents` (or `Modular.<Module>Integrations` — naming isn't fully consistent
  across modules, check the actual `.csproj` name) — the public contracts (`IIntegrationEvent` records)
  that other modules are allowed to reference. This is the only piece of a module another module should
  depend on directly.

`Modular.WebApi` is the single deployable host: it references every module's registration extension
(`RegisterCatalogModule`, `RegisterOrderModule`, `AddWarehouse`, `RegisterNotificationsModule`,
`RegisterCustomerModule`) and wires them up in `Program.cs`. There is no per-module API process — module
boundaries are enforced by project references and code organization, not by deployment.

`modular-monolith.AppHost` is the Aspire orchestrator (`AppHost.cs`) that spins up Postgres, RabbitMQ, and
Keycloak (with a bind-mounted realm export under `keycloak-config/`) and runs `Modular.WebApi` against
them for local dev.

### Use case pattern

Endpoints are Carter modules (`ICarterModule`) mapped in one file per use case, e.g.
`UseCases/Change/ChangeProductEndpoint.cs`. The handler pattern is consistent:

1. Endpoint maps an HTTP route, builds a MediatR command from the request DTO, sends it via `ISender`,
   and converts the `ErrorOr<T>` result to an `IResult` with `.ToResult(...)` (see
   `Modular.Common.ErrorOrExtensions`) — errors map to Problem Details (400/404/500 by `ErrorType`).
2. Command + validator + handler usually live together in one file named after the use case (e.g.
   `ChangeProductCommand.cs` contains the validator, the `internal sealed record` command, and the
   handler). Handlers talk directly to the module's `DbContext`.
3. Endpoints require permission via `RequireAuthorization(policy => policy.RequirePermission(Permissions.X))`,
   where `Permissions` is a per-module static class of `"module:action"` strings.

### Domain events → integration events (outbox pattern)

Modules using EF Core (Catalog, Orders, Notifications, Customers) follow an outbox flow:

1. Aggregate roots derive from `Modular.Common.AggregateRoot` and call `RaiseEvent(IDomainEvent)`.
2. `EventsToOutboxMessagesInterceptors` (an EF `SaveChangesInterceptor`, registered per-`DbContext`) runs
   on every `SaveChanges`, pulls pending domain events off tracked aggregates, and serializes them into an
   `OutboxMessages` table (Newtonsoft.Json with `TypeNameHandling.All`, so the type is embedded).
3. A per-module Quartz job (`<Module>.Infrastructure/BackgroundJobs/ProcessOutboxMessagesJob`, registered
   via `Register<Module>sBackgroundJobs()`) polls unprocessed outbox rows on a timer, deserializes them,
   runs them through a module-specific `DomainToIntegrationEventConverter` (a `switch` mapping each known
   domain event type to its public integration event), and publishes via MassTransit
   (`IPublishEndpoint.Publish`) wrapped in a Polly resilience pipeline (retry, exponential backoff+jitter —
   pipeline registered under `Constants.ResiliencePipelineName`).
4. Other modules consume integration events with MassTransit consumers registered via each module's
   `Add<Module>Consumers(...)` extension, wired up centrally in `Modular.WebApi/Program.cs` inside
   `builder.AddMassTransitRabbitMq(...)`.

When adding a new domain event that should cross module boundaries: add the domain event, add its public
integration event record in `Modular.<Module>.IntegrationEvents`, and add a case to that module's
`DomainToIntegrationEventConverter` — the outbox job throws `InvalidOperationException` for unmapped event
types, so a missed case is a runtime failure, not a compile error.

### Warehouse module is the exception

Warehouse mixes two persistence strategies: it has a conventional EF Core `DbContext` (confusingly also
named `OrderDbContext`, distinct from `Modular.Orders.OrderDbContext` — check the namespace, not just the
class name, when working across these two) for the order-shipping side, *and* it uses Marten for event
sourcing (`AddWarehouseConsumers` configures `AddMarten` with `StreamIdentity.AsString` and a
`DatabaseSchemaName` of `"Warehouse"`), publishing through `IntegrationEventPublisher` subscribed to Marten
projections instead of the outbox/Quartz pattern used elsewhere. Don't assume Catalog/Orders conventions
(outbox table, `ProcessOutboxMessagesJob`) apply here.

### Authorization

Keycloak issues JWTs; `KeycloakRolesClaimsTransformation` (`Modular.Authorization`) reads the
`resource_access` claim for the `eshop-public` client and maps its `roles` into
`CustomClaimTypes.Permission` claims. `PermissionExtensions.RequirePermission(...)` builds the
authorization policy each endpoint uses. Permission strings are defined per-module in
`Authorization/Permissions.cs` as `"<module>:<action>"` (e.g. `catalog:update`).

### Database

All modules share one Postgres database (`eshop`, provisioned by Aspire) but use separate schemas and a
per-schema `__EFMigrationsHistory` table (set via `MigrationsHistoryTable(..., <Module>DbContext.Schema)`
in each module's `AddDbContext` call). Migrations for every module's `DbContext` are applied at startup in
`Modular.WebApi/Program.cs` (`Database.MigrateAsync()` per context) — there's no separate migration step in
deployment.
