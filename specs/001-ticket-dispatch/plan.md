# Implementation Plan: Ticket Management & Dispatch Optimization

**Branch**: `001-ticket-dispatch` | **Date**: 2025-11-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-ticket-dispatch/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

GridPulse must extend the outage portal so operators can generate/manage outage tickets, dispatchers can optimize crew assignments, and crews (future app) receive actionable updates. The solution introduces ticketing domain entities, dispatch-ranking services fed by simulated telemetry, and placeholder delivery endpoints for the forthcoming crew mobile app. We will build new application services and repositories (Domain/Application/Infrastructure), expose Minimal API endpoints plus typed Blazor client calls, add operator/dispatcher UI flows with Radzen components, and seed mock GPS/assignment data so Aspire environments can demo the workflow end-to-end.

## Technical Context

**Language/Version**: .NET 10 / C# 13 (ASP.NET Core 9, Blazor Interactive Auto)  
**Primary Dependencies**: .NET Aspire 13, Minimal APIs, EF Core 9 + Npgsql, Radzen components, Polly & OpenTelemetry via `GridPulse.ServiceDefaults`  
**Storage**: Aspire-provisioned PostgreSQL (tickets, crews, recommendation state) + in-memory mock telemetry feed persisted via EF for auditing  
**Testing**: xUnit + FluentAssertions, EF Core in-memory + Testcontainers, bUnit/Playwright for UI, contract/integration tests via `WebApplicationFactory`, Aspire smoke (`aspire run`)  
**Target Platform**: Aspire AppHost orchestrating `GridPulse.WebApi` + `GridPulse.Web` (Interactive Auto UI) with HTTPS endpoints  
**Project Type**: Clean Architecture multi-project (Domain, Application, Infrastructure, WebApi, Web, ServiceDefaults, Tests)  
**Performance Goals**: Ticket creation <2s end-to-end, dispatch recommendations <5s after ticket ready, crew acknowledgement propagation <30s  
**Constraints**: API p95 <200 ms for ticket list/write paths, 60-second telemetry refresh cadence, no direct DB access from UI, feature flagged for staged rollout, temporary "auto-approved" security mocked via default user context until Entra auth lands  
**Scale/Scope**: Up to 10k concurrent tickets, ~50 dispatcher seats, 100+ crews with simulated location feeds, 3 new API groups + UI grids/forms + telemetry health checks

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Gate 1 – Clean Architecture Boundaries**: New entities (`Ticket`, `DispatchRecommendation`, `Crew`, `AssignmentEvent`) land in `GridPulse.Domain`. Application layer adds abstractions (`ITicketRepository`, `ITicketAutomationService`, `IDispatchOptimizationService`, `ICrewTelemetryFeed`) plus DTOs. Infrastructure implements repositories, telemetry simulators, and EF Core migrations but never leaks EF types upward. Hosts (WebApi/Web) only depend on application services via DI. No RFCs required because we stay within existing project boundaries.
- **Gate 2 – ServiceDefaults & Observability**: `GridPulse.ServiceDefaults` gains meters/log scopes for ticket automation, recommendation scoring, and crew delivery. Health checks added for PostgreSQL ticket DB, telemetry simulator heartbeat, and assignment push queue. HttpClient resilience profiles reused for typed API client calls (UI) plus any external webhooks; no bespoke middleware permitted in WebApi/Web.
- **Gate 3 – API + Typed Client Contracts**: Minimal APIs: `POST /api/tickets`, `GET /api/tickets`, `PATCH /api/tickets/{id}/status`, `GET /api/dispatch/recommendations`, `POST /api/dispatch/assignments`, `POST /api/crews/{id}/status`. DTOs live in `GridPulse.Application.Models` and flow through `GridPulseApiClient` (extensions for operator + dispatcher pages). UI pages (Radzen grids/forms) consume typed client methods only.
- **Gate 4 – Testing & Data Readiness**: Unit tests for ticket state machine, recommendation scoring, telemetry simulator, DTO mappers. Contract/integration tests for each API via `WebApplicationFactory` + seeded Postgres container. bUnit/Playwright flows for operator ticket grid + dispatcher board. EF migration adds ticket tables plus seed script for mock crews; Aspire `seed-data` project populates demo telemetry.
- **Gate 5 – UI & API Lite Constitutions**: UI work complies with `UIS/constitution.md` (typed client-only calls, Radzen-first, accessibility) and API surface follows `API/constitution.md` (Minimal API grouping, ProblemDetails, repository isolation). Tasks will cite these docs whenever Blazor pages or Minimal APIs change. Security-sensitive flows introduce an `IUserContext` abstraction plus mock identity provider so Entra wiring can replace it without touching UI/API boundaries.

> **Post-Phase-1 Check (2025-11-20)**: Research, data model, contracts, and quickstart deliverables introduce no new boundary violations; all five gates remain satisfied.

**Security Placeholder Strategy**: Until Entra authentication and role claims land, the solution uses an `IUserContext` abstraction plus mock identity provider that always returns an authenticated operator/dispatcher. All Minimal APIs and Blazor components must depend on this accessor so swapping in real Entra-backed claims later only requires replacing the provider rather than rewriting endpoints or UI guards.

## Project Structure

### Documentation (this feature)

```text
specs/001-ticket-dispatch/
├── plan.md              # This file (/speckit.plan output)
├── research.md          # Phase 0 research + decisions
├── data-model.md        # Phase 1 entity + state diagrams
├── quickstart.md        # Phase 1 operator/dispatcher runbook
├── contracts/           # OpenAPI/DTO excerpts & mock payloads
└── tasks.md             # Phase 2 task plan (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── GridPulse.Domain/
│   ├── Entities/
│   │   ├── Ticket.cs
│   │   ├── DispatchRecommendation.cs
│   │   ├── Crew.cs
│   │   └── AssignmentEvent.cs
│   └── Enums/
│       ├── TicketStatus.cs
│       ├── TicketPriority.cs
│       └── CrewSkill.cs
├── GridPulse.Application/
│   ├── Abstractions/
│   │   ├── ITicketRepository.cs
│   │   ├── ITicketAutomationService.cs
│   │   ├── IDispatchOptimizationService.cs
│   │   └── ICrewTelemetryFeed.cs
│   ├── Models/
│   │   ├── TicketDto.cs
│   │   ├── DispatchRecommendationDto.cs
│   │   └── CrewStatusDto.cs
│   └── Services/
│       ├── TicketWriteService.cs
│       ├── TicketQueryService.cs
│       └── DispatchOptimizationService.cs
├── GridPulse.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/
│   │   │   ├── TicketConfiguration.cs
│   │   │   └── DispatchRecommendationConfiguration.cs
│   │   ├── Migrations/2025XXXX_AddTicketsAndDispatch.cs
│   │   └── SeedData/TicketSeed.cs
│   ├── Repositories/
│   │   ├── EfTicketRepository.cs
│   │   └── EfDispatchRepository.cs
│   └── Telemetry/
│       └── MockCrewTelemetryFeed.cs
├── GridPulse.WebApi/
│   ├── Endpoints/TicketsEndpointGroup.cs
│   ├── Endpoints/DispatchEndpointGroup.cs
│   └── Contracts/
│       └── TicketMapper.cs
├── GridPulse.Web/
│   ├── Components/Pages/
│   │   ├── Tickets.razor
│   │   └── DispatchBoard.razor
│   ├── Components/Organisms/
│   │   └── TicketTimeline.razor
│   └── Services/
│       └── GridPulseApiClientExtensions.cs
├── GridPulse.ServiceDefaults/
│   └── Observability/TicketingDiagnostics.cs
└── GridPulse.Tests.Unit/
    ├── Application/TicketWriteServiceTests.cs
    ├── Application/DispatchOptimizationServiceTests.cs
    ├── WebApi/TicketEndpointTests.cs
    └── Web/Components/TicketsPageTests.cs

tests/ (Playwright)
└── GridPulse.Web.Tests.Playwright/
    └── TicketsAndDispatch.spec.ts
```

**Structure Decision**: Extend existing Clean Architecture solution; no new top-level projects required. Domain/Application/Infrastructure gain new entities/services/repositories and EF migrations. WebApi exposes grouped endpoints; Web adds Radzen-based operator/dispatcher pages; ServiceDefaults centralizes new telemetry. Tests live alongside existing test assemblies (unit + Playwright) using current folder structure.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| *None* |  |  |
