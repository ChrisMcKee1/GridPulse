# PostgreSQL Migration & Refactor Plan

## Microsoft Learn guidance
- **Hosting resource modeling & containers** — Use `Aspire.Hosting.PostgreSQL` to add `PostgresServerResource`/`PostgresDatabaseResource` instances plus optional PgAdmin/PgWeb helpers directly inside `AppHost.cs` so the Aspire orchestrator provisions a local dockerized server and wires dependent projects with connection metadata.[^1]
- **EF Core client integration** — Use `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` to register DbContexts with `builder.AddNpgsqlDbContext<TContext>("postgresdb")`, gaining baked-in health checks, retries, OpenTelemetry logging/metrics, and configuration binding through `Aspire:Npgsql:EntityFrameworkCore:PostgreSQL` settings.[^1]

## Current state inventory
1. **Oracle placeholders** — `src\GridPulse.Infrastructure\Options\OracleOptions.cs`, the `services.Configure<OracleOptions>` call in `ServiceCollectionExtensions`, and multiple doc references (`README.md`, `docs/architecture.md`, `docs/features.md`) advertise an Oracle roadmap but no concrete implementation.
2. **Data access** — `InMemoryOutageRepository` is the only `IOutageRepository` implementation; there is no EF Core model, no DbContext, and no database migrations.
3. **AppHost** — `src\GridPulse.AppHost\AppHost.cs` only composes the API and Web projects. No external resources (databases, secrets) are defined, so nothing is currently wired for persistence.

## Removal & cleanup targets
| Area | Action |
| --- | --- |
| Infrastructure | Delete `OracleOptions` and remove its configuration binding registrations. |
| Documentation | Replace Oracle-forward-looking statements with Postgres/EF verbiage to avoid conflicting guidance once the refactor starts. |
| Dependency lists | Remove any pending Oracle NuGet placeholders from issue trackers/README checklists to keep focus on the Postgres goal. |
| Seed data | Plan to drop `InMemoryOutageRepository` once EF Core delivers equivalent seed/migration scripts; keep temporarily for feature parity during transition. |

## Postgres + EF Core implementation strategy
1. **Introduce shared persistence abstractions**
   - Create `GridPulse.Infrastructure.Persistence` folder with `GridPulseDbContext : DbContext` plus configuration classes for Outage aggregates.
   - Map domain types using EF Core configuration (e.g., `IEntityTypeConfiguration<Outage>` and owned collections for `OutageEvent`).
2. **Package dependencies**
   - Add `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `Npgsql.EntityFrameworkCore.PostgreSQL`, and `Microsoft.EntityFrameworkCore.Design` to `GridPulse.WebApi` (data owner) so migrations can be created/run. These packages enable the `AddNpgsqlDbContext` helper and tooling described in the Microsoft guidance.[^1]
   - Add `Aspire.Hosting.PostgreSQL` to `GridPulse.AppHost` to declare and manage the Postgres container resource.[^1]
3. **DbContext registration**
   - In `GridPulse.WebApi/Program.cs`, register the DbContext via `builder.AddNpgsqlDbContext<GridPulseDbContext>("gridpulse-db")` before calling `builder.Services.AddInfrastructureServices` so DI can inject it into repositories. Keep `AddProblemDetails`, `AddOpenApi`, and the CORS policy untouched.
   - Update `AddInfrastructureServices` to accept `IHostApplicationBuilder` or `IServiceCollection` and register `GridPulseDbContext`-backed repositories (replacing the in-memory singleton once validated).
4. **Database resource in AppHost**
   - Extend `AppHost.cs`:
     ```csharp
     var postgres = builder.AddPostgres("gridpulse-postgres")
                           .WithDataVolume();
     var outageDb = postgres.AddDatabase("gridpulse-db");

     var api = builder.AddProject<Projects.GridPulse_WebApi>("gridpulse-api")
                      .WithReference(outageDb);
     ```
   - Keep the existing `.WithReference(api)` on the Blazor client so it inherits the connection info needed for typed HttpClient base address resolution.
5. **Configuration & secrets**
   - Store the connection name (`gridpulse-db`) inside `appsettings.Development.json` under `ConnectionStrings` for non-Aspire scenarios; Dev containers will receive the secret via Aspire wiring automatically.
   - For production, either point to Azure Database for PostgreSQL Flexible Server or reuse the same `AddPostgres` pattern with environment-specific parameters.
6. **Repository refactor**
   - Implement `EfOutageRepository : IOutageRepository` using the DbContext; remove the in-memory implementation after parity tests pass. Keep query projections close to the database layer to minimize serialization cost.
   - Ensure asynchronous LINQ queries include `AsNoTracking()` for read scenarios since all operations are read-only today.
7. **Migrations & Seeding**
   - Scaffold an initial migration (`dotnet ef migrations add InitialOutages --project src/GridPulse.WebApi --startup-project src/GridPulse.WebApi`).
   - Use `modelBuilder.Entity<Outage>().HasData(...)` or an injectable seed service triggered at startup to mirror the current sample outage so dashboards stay functional.
8. **Observability & health**
   - Rely on the automatic DbContext health checks and OpenTelemetry wiring that the Aspire EF integration supplies; expose the `/health` endpoint already registered in `Program.cs` to bubble readiness through AppHost.[^1]

## Dev workflow adjustments
1. **Local** — Run `aspire run --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj`; Docker (already running) will host the Postgres container declared in AppHost. Use PgAdmin/PgWeb add-ons if interactive inspection is required.
2. **CI/CD** —
   - Add `dotnet ef database update --project src/GridPulse.WebApi -- --apphost` to integration pipelines or rely on Aspire's deployment story if targeting Azure Container Apps.
   - Persist migration bundles alongside the API artifacts for repeatable deployments.

## Testing & rollout
1. **Unit tests** — Create repository tests under `GridPulse.Tests.Unit` that target an in-memory Npgsql container (use `Testcontainers` or `Respawn` for cleanup) to lock in query semantics.
2. **Integration smoke tests** — Extend the existing Aspire pipeline to wait on the Postgres health check before running API endpoint tests.
3. **Cutover** —
   - Stage 1: keep both repositories registered behind a feature flag to compare responses.
   - Stage 2: remove the in-memory implementation and delete Oracle remnants once the EF-backed repository is proven in QA.

## Risks & mitigations
| Risk | Mitigation |
| --- | --- |
| EF model drift from domain records | Use configuration classes plus snapshot tests to ensure columns stay aligned with immutable records. |
| Connection secrets leakage | Let Aspire manage credentials; for non-Aspire hosting, load secrets from `dotnet user-secrets` or Azure Key Vault references instead of checked-in JSON. |
| Docker volume growth | Use `.WithDataVolume()` only for dev; in CI use ephemeral containers to avoid growth, and rely on managed Azure Postgres in prod. |

## Immediate next actions
1. Remove `OracleOptions` and related documentation references to prevent mixed messaging.
2. Add the `Aspire.Hosting.PostgreSQL` and `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` packages and check in the baseline `GridPulseDbContext` plus initial migration.
3. Wire the DbContext-backed repository into `AddInfrastructureServices` and delete `InMemoryOutageRepository` once acceptance tests confirm parity.
4. Update README + architecture docs to describe the new Postgres stack and operational expectations.

[^1]: Microsoft Learn — [Aspire PostgreSQL Entity Framework Core integration](https://learn.microsoft.com/en-us/dotnet/aspire/database/postgresql-entity-framework-integration)
