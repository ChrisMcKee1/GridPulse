# Copilot Instructions for GridPulse

## Architecture at a Glance
- Clean Architecture split lives in `src/`: `GridPulse.Domain` (entities/enums), `GridPulse.Application` (service abstractions + models), `GridPulse.Infrastructure` (data access + options), `GridPulse.WebApi` (minimal API), `GridPulse.Web` (Blazor Interactive Auto + WASM client), `GridPulse.AppHost` (Aspire 13 orchestrator), and `GridPulse.Tests.Unit`.
- Start in `GridPulse.AppHost/AppHost.cs` to see how services hang together: the AppHost exposes the API on port 7143 and references the Blazor web front end via `WithReference(api)`.
- Data access is stubbed via `Infrastructure/Repositories/InMemoryOutageRepository.cs`. The long-term plan is Oracle via `System.Data.Common`, so keep repository interfaces (`Application/Abstractions`) storage-agnostic.
- API surface is grouped under `/api/outages` in `GridPulse.WebApi/Program.cs`. Minimal APIs + dependency-injected services (`IOutageReadService`) are the norm, and we rely on `MapGroup` + typed DI parameters—mirror that pattern.
- The UI uses Blazor Interactive Auto (`GridPulse.Web/Program.cs`) with a typed HttpClient (`Services/GridPulseApiClient.cs`) whose base address comes from `Api:BaseAddress` configuration or defaults to `https://localhost:7143`.

## Developer Workflows
- Day-to-day execution uses the Aspire CLI (see `.github/instructions/aspire-cli.instructions.md` for full reference):
  - `aspire run --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj`
  - `aspire publish --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj -o ./publish -e Production`
  - `aspire add <resource>` when wiring new integrations.
- Scalar replaced Swagger for interactive API docs. In development the API host publishes OpenAPI + Scalar (`/scalar/v1`), so keep `builder.Services.AddOpenApi()` and `app.MapScalarApiReference()` intact when adding routes.

## Project Conventions
- Domain types are immutable records/classes with `init` setters. When you introduce new entities, put them in `GridPulse.Domain.Entities` and add enums under `GridPulse.Domain.Enums`.
- Application layer exposes DTOs/records defined in `Application/Models` and registers services via `ServiceCollectionExtensions`. Use dependency injection (Microsoft.Extensions) only—no static singletons.
- Infrastructure handles configuration binding (`OracleOptions`) and repository implementations. Keep new data sources behind interfaces declared in `Application.Abstractions`.
- Minimal APIs prefer explicit groups, `WithTags`, and returning `Results`. Use `builder.Services.AddProblemDetails()` and `app.UseExceptionHandler()` rather than custom middleware.
- Blazor components import DTOs through `_Imports.razor`. When calling APIs, reuse the typed client rather than `HttpClient` directly so the base address logic stays centralized.
- Configuration lives in each project’s `appsettings*.json`. For the UI, `Api:BaseAddress` must match the AppHost-exposed API port (7143 unless you change `AppHost.cs`).
- Tests are currently minimal (`GridPulse.Tests.Unit`). If you add new services, drop fast unit tests here using xUnit.

## Integration Notes & Future Work
- Oracle connectivity is not implemented yet—`OracleOptions` in Infrastructure is the placeholder. When you add it, follow the Managed Identity + `DefaultAzureCredential` plan documented in the PRD/research notes.
- Authentication/authorization is also pending; plan to integrate Microsoft Entra (B2C for customers, Entra ID for operators) at both API and Blazor layers when scaffolding security features.
- Radzen components are expected for richer UI; keep Blazor render mode in Interactive Auto so you can prerender server-side but hand off to WASM for interactivity.

Use these guardrails whenever you contribute—if anything here feels incomplete, flag it so we can tighten the guidance.
