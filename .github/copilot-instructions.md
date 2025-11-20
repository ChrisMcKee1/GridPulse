# Copilot Instructions for GridPulse

## Architecture & Boundaries
- Clean Architecture lives under `src/`: `GridPulse.Domain` (immutable entities/enums), `GridPulse.Application` (abstractions + DTOs + services such as `TicketAutomationService`), `GridPulse.Infrastructure` (EF Core persistence, seeders, telemetry), `GridPulse.WebApi` (Minimal APIs + hosted workers), `GridPulse.Web` (Blazor Interactive Auto UI + Radzen components), `GridPulse.ServiceDefaults` (shared telemetry/resilience), and `GridPulse.AppHost` (Aspire orchestrator wiring API→Web on 7143/7210).
- Data flow: outages land in PostgreSQL via CSV seed (`OutageCsvSampleDataSeeder`), automation worker (`GridPulse.WebApi/Workers/TicketAutomationWorker.cs`) converts outages into tickets through `ITicketAutomationService`, repositories (`EfTicketRepository`) persist state, and the Blazor UI consumes typed DTOs via `GridPulseApiClientExtensions`.
- ServiceDefaults is non-optional—every host must call `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()` so OpenTelemetry, health checks, and HttpClient resilience stay consistent.

## Constitutions & Compliance
- The parent **GridPulse Constitution** (`.specify/memory/constitution.md`) governs Clean Architecture, ServiceDefaults, API-first contracts, typed clients, and observability/testing gates. Reference it in plans/specs/PRs whenever touching cross-layer concerns.
- The **API Lite Constitution** (`API/constitution.md`) inherits from the parent and dictates Minimal API grouping, ProblemDetails responses, repository isolation, and Scalar/OpenAPI updates. Any API/typed-client work must cite this doc.
- The **UI Lite Constitution** (`UIS/constitution.md`) covers Blazor/Radzen rules: Interactive Auto render mode, typed client usage, DTO-only state, accessibility, and telemetry hooks. UI changes must explicitly reference this constitution plus the parent.
- Tasks/specs should mention which constitution sections are satisfied; reviewers expect violations to be resolved or justified via RFC.

## Everyday Workflows
- Run the whole stack with Aspire CLI: `aspire run --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj`. Publish or add resources with `aspire publish` / `aspire add …` per `.github/instructions/aspire-cli.instructions.md`.
- Tests live in `GridPulse.Tests.Unit`: `dotnet test src/GridPulse.Tests.Unit/GridPulse.Tests.Unit.csproj` exercises xUnit + bUnit (UI tests rely on `ComponentTestBase`, remember to stub required Radzen JS calls).
- Seed data is applied automatically from `DatabaseInitializationExtensions.InitializeDatabaseAsync()`; when changing seeders, keep them idempotent because both `aspire run` and tests call them on startup.

## Implementation Patterns
- Repository interfaces belong in `Application/Abstractions`; implementations reside in `Infrastructure/Repositories`. Never reference EF Core or DbContext from Application or Web projects.
- Minimal APIs are grouped (see `GridPulse.WebApi/Endpoints/TicketsEndpointGroup.cs`) with `MapGroup`, `.WithTags`, `Results` return helpers, and `AddProblemDetails`. New endpoints should follow that pattern and register via `Program.cs` rather than ad-hoc controllers.
- Background processing runs via hosted services inside WebApi (e.g., `TicketAutomationWorker`). Keep options objects under `Options/…` or alongside the worker and bind via `IOptions<T>`.
- Blazor UI uses Radzen components plus typed data clients. Any network call must go through `GridPulse.Web/GridPulse.Web/Services/GridPulseApiClient.cs` + corresponding extension methods so the base URL + auth logic stay centralized; never spin up raw `HttpClient` inside components.
- Styling belongs in `GridPulse.Web/wwwroot/app.css`; follow existing ticket styles (`.tickets-page`, `.tickets-detail-card`) when introducing new UI to keep the operator/dispatcher experiences cohesive.

## Testing & Quality Bars
- When adding services or DTO mappers, add matching unit tests under `GridPulse.Tests.Unit/Application` or `…/Web/Services`. Component tests should inherit from `ComponentTestBase` so Radzen services and JSInterop mocks are registered.
- API contract coverage goes in `GridPulse.Tests.Unit/WebApi/*Tests.cs` using `WebApplicationFactory<Program>`. Seed data must support these tests, so update `TicketSeed` when new workflows need deterministic fixtures.

## Integration & Future Considerations
- PostgreSQL is provisioned by Aspire; connection strings live under `GridPulse.WebApi/appsettings*.json`. When targeting Azure, swap to Managed Identity but keep repository APIs identical.
- Authentication is currently `MockUserContext` (see `GridPulse.Infrastructure/Auth`). When adding authorization checks, use the abstraction so Entra wiring can replace it without touching higher layers.
- Radzen UI is the standard; keep components in Interactive Auto mode and prefer server-side prerender + WASM handoff for responsive dashboards.

If any part of this guidance is unclear or missing context (e.g., additional workflows, dispatcher UI specifics), please call it out so we can refine these instructions.
