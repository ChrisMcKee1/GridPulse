# Copilot Instructions for GridPulse

## Architecture at a Glance
- Clean Architecture split lives in `src/`: `GridPulse.Domain` (entities/enums), `GridPulse.Application` (service abstractions + models), `GridPulse.Infrastructure` (data access + options), `GridPulse.WebApi` (minimal API), `GridPulse.Web` (Blazor Interactive Auto + WASM client), `GridPulse.ServiceDefaults` (shared Aspire defaults), `GridPulse.AppHost` (Aspire 13 orchestrator), and `GridPulse.Tests.Unit`.
- Start in `GridPulse.AppHost/AppHost.cs` to see how services hang together: the AppHost exposes the API on port 7143 and references the Blazor web front end via `WithReference(api)`.
- Data access runs through `Infrastructure/Persistence/GridPulseDbContext` + `Repositories/EfOutageRepository.cs`, backed by an Aspire-provisioned PostgreSQL container. Keep repository interfaces (`Application/Abstractions`) storage-agnostic so we can swap to Azure Database for PostgreSQL later.
- API surface is grouped under `/api/outages` in `GridPulse.WebApi/Program.cs`. Minimal APIs + dependency-injected services (`IOutageReadService`) are the norm, and we rely on `MapGroup` + typed DI parameters—mirror that pattern.
- The UI uses Blazor Interactive Auto (`GridPulse.Web/Program.cs`) with a typed HttpClient (`Services/GridPulseApiClient.cs`) whose base address comes from `Api:BaseAddress` configuration or defaults to `https://localhost:7143`.
- `GridPulse.ServiceDefaults` centralizes `AddServiceDefaults()`, OpenTelemetry wiring, health endpoints, HttpClient resilience, and request/response body enrichment for the Aspire dashboard. Every ASP.NET Core project must reference it and call `builder.AddServiceDefaults()` + `app.MapDefaultEndpoints()`.

## Developer Workflows
- Day-to-day execution uses the Aspire CLI (see `.github/instructions/aspire-cli.instructions.md` for full reference):
  - `aspire run --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj`
  - `aspire publish --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj -o ./publish -e Production`
  - `aspire add <resource>` when wiring new integrations.
- Scalar replaced Swagger for interactive API docs. In development the API host publishes OpenAPI + Scalar (`/scalar/v1`), so keep `builder.Services.AddOpenApi()` and `app.MapScalarApiReference()` intact when adding routes.
- When introducing new HTTP entry points, add them to ServiceDefaults (not ad-hoc middleware) if they require shared telemetry, logging, or health behavior.

## Project Conventions
- Domain types are immutable records/classes with `init` setters. When you introduce new entities, put them in `GridPulse.Domain.Entities` and add enums under `GridPulse.Domain.Enums`.
- Application layer exposes DTOs/records defined in `Application/Models` and registers services via `ServiceCollectionExtensions`. Use dependency injection (Microsoft.Extensions) only—no static singletons.
- Infrastructure handles persistence (`GridPulseDbContext`, EF configurations/migrations) and repository implementations. Keep new data sources behind interfaces declared in `Application.Abstractions`.
- Minimal APIs prefer explicit groups, `WithTags`, and returning `Results`. Use `builder.Services.AddProblemDetails()` and `app.UseExceptionHandler()` rather than custom middleware.
- Blazor components import DTOs through `_Imports.razor`. When calling APIs, reuse the typed client rather than `HttpClient` directly so the base address logic stays centralized.
- Configuration lives in each project’s `appsettings*.json`. For the UI, `Api:BaseAddress` must match the AppHost-exposed API port (7143 unless you change `AppHost.cs`).
- Shared observability or resilience changes should be made in `GridPulse.ServiceDefaults` first so both WebApi and Blazor hosts stay consistent.
- Tests are currently minimal (`GridPulse.Tests.Unit`). If you add new services, drop fast unit tests here using xUnit.

## Integration Notes & Future Work
- PostgreSQL connectivity is wired via Aspire locally; when targeting Azure, promote to Azure Database for PostgreSQL + Managed Identity and keep secrets in Key Vault or environment-specific configuration.
- Authentication/authorization is also pending; plan to integrate Microsoft Entra (B2C for customers, Entra ID for operators) at both API and Blazor layers when scaffolding security features.
- Radzen components are expected for richer UI; keep Blazor render mode in Interactive Auto so you can prerender server-side but hand off to WASM for interactivity.

Use these guardrails whenever you contribute—if anything here feels incomplete, flag it so we can tighten the guidance.
