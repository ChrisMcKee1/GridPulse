<!--
Sync Impact Report
Version change: 1.0.0 → 1.1.0
Modified principles:
- None
Added sections:
- Layer Constitutions (references to UI/API lite constitutions)
Removed sections:
- None
Templates requiring updates:
- None
Follow-up TODOs:
- None
-->

# GridPulse Constitution

## Core Principles

### I. Clean Architecture Boundaries

- Code MUST respect the Domain → Application → Infrastructure → Host dependency rule; shortcuts or cross-layer references are rejected in review.
- Domain entities stay immutable (init-only setters) and never depend on infrastructure packages.
- Application projects publish service/repository abstractions plus DTOs; infrastructure implements them behind DI registrations without leaking EF types upward.
- Host projects compose functionality exclusively through DI—any cross-layer leak or static singleton requires an approved RFC.

**Rationale**: Preserving inversion of control keeps persistence providers (PostgreSQL today, Azure services tomorrow) and host shells swappable without regressions.

### II. Aspire Service Defaults Everywhere

- Every runnable project references `GridPulse.ServiceDefaults`, calls `builder.AddServiceDefaults()`, and maps default endpoints before adding bespoke middleware.
- Observability, health checks, HttpClient resilience, and service discovery changes are centralized in ServiceDefaults; duplicating this logic elsewhere is prohibited.
- Typed HttpClients MUST rely on Aspire service discovery (`https+http://gridpulse-api`, etc.); raw `new HttpClient()` usage is forbidden.

**Rationale**: Shared defaults guarantee consistent OpenTelemetry traces, health surfaces, and network routing across the distributed topology.

### III. API-First Contracts

- Minimal APIs expose grouped routes under `/api/...`, annotated with `WithTags` and explicit DI parameters.
- Every new endpoint updates OpenAPI metadata and remains discoverable in Scalar (`/scalar/v1`) while honoring `AddProblemDetails()` semantics.
- Responses return typed `Results` (or strongly typed equivalents) and emit ProblemDetails on failures; ad-hoc JSON writers are not allowed.

**Rationale**: API-first discipline keeps Blazor, future mobile clients, and partner integrations aligned on predictable contracts.

### IV. Blazor Clients via Typed APIs

- UI code interacts with the backend exclusively through `GridPulseApiClient` (or successor typed clients) configured via DI.
- The render mode stays Blazor Interactive Auto with Radzen-based components composing DTOs produced by the application layer.
- UI projects never reference EF entities or database contexts; all state comes from application models and typed API responses.

**Rationale**: Typed clients and DTO-only flows keep the UI independently testable and prevent data-layer coupling.

### V. Observability & Test Discipline

- New features instrument structured logs/traces through ServiceDefaults and add health/readiness checks when applicable.
- `dotnet build`, `dotnet test`, and an `aspire run` smoke must pass locally and in CI before merge.
- Services, repositories, and clients gain fast unit tests in `GridPulse.Tests.Unit`; integration or contract tests accompany API additions.

**Rationale**: Enforced telemetry plus fast tests catch regressions before Aspire deployments amplify them.

## Operational Guardrails

- Persistence flows through `GridPulseDbContext` and repository abstractions; alternative stores (Azure Database for PostgreSQL, Cosmos DB, etc.) must sit behind application interfaces with documented migration plans.
- Configuration and secrets ride through Aspire-managed environment settings—no hard-coded credentials or inline connection strings may enter source control.
- Scalar + OpenAPI stay enabled in development; disabling documentation or ProblemDetails requires explicit architect approval and a remediation plan.
- The Aspire CLI (`aspire run`, `aspire publish`, `aspire deploy`, `aspire add`) is the authoritative path for orchestration; scripts and docs must reference these commands.
- Cross-cutting telemetry or HttpClient policies belong in `GridPulse.ServiceDefaults`; feature hosts cannot diverge without updating that project first.

## Layer Constitutions

- **UI Lite Constitution** – `UIS/constitution.md` captures Blazor-specific guardrails (typed clients, render mode discipline, Radzen component requirements, accessibility). UI work MUST reference it plus this parent document. See also `docs/blazor-radzen-lessons-learned.md` for critical component usage patterns.
- **API Lite Constitution** – `API/constitution.md` governs minimal API practices (routing, ProblemDetails, repository boundaries, telemetry). API changes MUST satisfy both constitutions.

## Workflow Expectations

1. **Constitution Check (per /speckit.plan)** – Plans must show the outcome of four gates: Clean Architecture dependencies verified, ServiceDefaults/observability impact documented, API + typed client contracts enumerated, and testing/data-readiness steps funded.
2. **Specifications (per /speckit.spec)** – User stories stay prioritized, independently testable, and describe their API endpoints, DTOs, and UI slices so Principle III/IV implications are explicit.
3. **Tasks (per /speckit.tasks)** – Tasks are grouped by user story and explicitly call out instrumentation, testing, and data/repository work; cross-cutting telemetry or client updates get their own tasks rather than being implicit.
4. **Reviews** – Every PR review checklist verifies constitution gates plus documentation updates (README/docs) when principles are touched; violations block merge until resolved.

## Governance

- This constitution supersedes ad-hoc practices. Contributors must reference it in plans, specs, tasks, and PR reviews.
- Amendments require an RFC describing motivation, migration impact, and template changes; once approved, update this file and dependent templates in the same PR.
- Versioning follows SemVer: PATCH for clarifications, MINOR for new principles/sections, MAJOR for breaking governance changes.
- Compliance reviews occur at feature kickoff (plan), design (spec), implementation planning (tasks), and PR merge. Non-compliance halts rollout until remediated.

**Version**: 1.1.0 | **Ratified**: 2025-11-20 | **Last Amended**: 2025-11-20
