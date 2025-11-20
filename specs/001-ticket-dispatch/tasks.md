---
description: "Task list for Ticket Management & Dispatch Optimization"
---

# Tasks: Ticket Management & Dispatch Optimization

**Input**: Design documents from `/specs/001-ticket-dispatch/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required per spec for APIs (contract/integration), application services (unit), and UI (bUnit/Playwright).

**Organization**: Tasks are grouped by user story so each increment can be implemented and tested independently.

**Constitution Alignment**: Every story enforces Clean Architecture boundaries, updates typed API clients + Minimal APIs together, wires telemetry through `GridPulse.ServiceDefaults`, and adds tests + docs per UI/API lite constitutions.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare workspace, dependencies, and baseline environments before writing feature code.

- [x] T001 Checkout or create feature branch `001-ticket-dispatch` and confirm `specs/001-ticket-dispatch/plan.md` + supporting docs are synced with main.
- [x] T002 [P] Restore solution dependencies with `dotnet workload restore ./GridPulse.sln` and `dotnet tool restore` so new projects build consistently.
- [x] T003 [P] Install/update Radzen and Playwright prerequisites by running `npm install` within `src/GridPulse.Web` and `npx playwright install` in `tests/GridPulse.Web.Tests.Playwright`.
- [x] T004 [P] Validate baseline `aspire run --project src/GridPulse.AppHost/GridPulse.AppHost.csproj` to ensure AppHost + PostgreSQL container are healthy before applying migrations.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain, persistence, telemetry, and infrastructure pieces every user story depends on.

- [x] T005 Create ticketing domain entities and enums (`Ticket`, `DispatchRecommendation`, `Crew`, `AssignmentEvent`, `TicketStatus`, `TicketPriority`, `CrewSkill`) under `src/GridPulse.Domain/Entities|Enums` with immutable records per constitution.
- [x] T006 [P] Add application-layer DTOs and abstractions (`ITicketRepository`, `ITicketAutomationService`, `IDispatchOptimizationService`, `ICrewTelemetryFeed`, `TicketDto`, `DispatchRecommendationDto`, `CrewStatusDto`) in `src/GridPulse.Application/Abstractions` and `src/GridPulse.Application/Models`.
- [x] T007 [P] Extend `GridPulseDbContext` plus Fluent configurations for all new tables and generate migration `2025XXXX_AddTicketsAndDispatch` in `src/GridPulse.Infrastructure/Persistence/Migrations` with seed-ready schema.
- [x] T008 [P] Implement repositories (`EfTicketRepository`, `EfDispatchRepository`) and register them via DI inside `src/GridPulse.Infrastructure/Repositories` and `src/GridPulse.WebApi/Program.cs`.
- [x] T009 [P] Add ServiceDefaults instrumentation (meters, logs, health checks for Postgres + telemetry feed + assignment queue) in `src/GridPulse.ServiceDefaults/Observability/TicketingDiagnostics.cs` and ensure hosts call the new extensions.
- [x] T010 [P] Implement `MockCrewTelemetryFeed` hosted service and `SeedData/TicketSeed.cs` routines inside `src/GridPulse.Infrastructure/Telemetry` + `SeedData` so Aspire environments replay crew GPS data.
- [x] T041 [P] Wire an `IUserContext` abstraction + mock provider that returns an auto-approved operator/dispatcher identity, and update Minimal APIs/Blazor DI registrations to resolve it (files: `src/GridPulse.Application/Abstractions/IUserContext.cs`, `src/GridPulse.Infrastructure/Auth/MockUserContext.cs`, `src/GridPulse.WebApi/Program.cs`, `src/GridPulse.Web/Program.cs`).
- [x] T042 Document and guard the placeholder auth by adding TODO hooks + feature flag settings (`appsettings.Development.json`, `docs/development.md`) so Entra-backed claims can replace the mock provider without touching endpoints/UI logic.

**Checkpoint**: Domain + infrastructure scaffolding ready; user stories can proceed.

---

## Phase 3: User Story 1 – Operator Manages Outage Tickets (Priority: P1) 🎯 MVP

**Goal**: Operators can see auto-generated tickets, edit details, and move them through Open → Closed with full audit history.

**Independent Test**: Seed outage feed, verify tickets auto-create within 2s, allow manual edits, and complete workflow transitions with timeline entries.

### Tests – US1 (write before implementation)

- [ ] T011 [P] [US1] Add unit tests covering `TicketWriteService` workflow transitions and duplicate-detection logic in `src/GridPulse.Tests.Unit/Application/TicketWriteServiceTests.cs`.
- [ ] T012 [P] [US1] Add API contract/integration tests for `/api/tickets` create/list/status endpoints using `WebApplicationFactory` in `src/GridPulse.Tests.Unit/WebApi/TicketEndpointTests.cs`.
- [ ] T013 [P] [US1] Add bUnit tests for `src/GridPulse.Web/Components/Pages/Tickets.razor` + `Components/Organisms/TicketTimeline.razor` covering grid filtering and promotion flow.

### Implementation – US1

- [ ] T014 [P] [US1] Implement `TicketWriteService`, `TicketQueryService`, and `TicketAutomationService` logic under `src/GridPulse.Application/Services` using new abstractions + emitting `AssignmentEvent` history.
- [ ] T015 [US1] Add `TicketAutomationWorker` hosted service in `src/GridPulse.WebApi/Workers/TicketAutomationWorker.cs` and wire it in `Program.cs` to subscribe to outage feed + publish metrics.
- [ ] T016 [US1] Build `/api/tickets` Minimal API group in `src/GridPulse.WebApi/Endpoints/TicketsEndpointGroup.cs` (POST list, GET list w/ filters, PATCH status) returning ProblemDetails on validation errors.
- [ ] T017 [P] [US1] Extend `GridPulseApiClient` and DTO mappers in `src/GridPulse.Web/Services/GridPulseApiClientExtensions.cs` so operator UI only hits typed endpoints.
- [ ] T018 [US1] Implement Radzen-based operator UI (`Tickets.razor`, `TicketTimeline.razor`) with loading/error states, duplicate ticket warnings, and hydration-safe state handling.
- [ ] T019 [US1] Add ticket automation seed + duplicate alert logic in `src/GridPulse.Infrastructure/SeedData/TicketSeed.cs` and ensure EF migration seeds baseline data for QA.

**Checkpoint**: MVP ready—operators can manage tickets independently.

---

## Phase 4: User Story 2 – Dispatcher Optimizes Crew Assignment (Priority: P2)

**Goal**: Dispatchers see ranked crew recommendations, can override, and publish assignments with audit + telemetry updates.

**Independent Test**: With mock telemetry running, dispatcher board shows ranked crews, allows override with justification, and produces assignment receipts stored in history.

### Tests – US2 (write before implementation)

- [ ] T020 [P] [US2] Add unit tests for `DispatchOptimizationService` scoring weights and stale telemetry handling in `src/GridPulse.Tests.Unit/Application/DispatchOptimizationServiceTests.cs`.
- [ ] T021 [P] [US2] Add integration tests for `/api/dispatch/recommendations` + `/api/dispatch/assignments` using `WebApplicationFactory` and seeded crews under `src/GridPulse.Tests.Unit/WebApi/DispatchEndpointTests.cs`.
- [ ] T022 [P] [US2] Expand Playwright scenario `tests/GridPulse.Web.Tests.Playwright/TicketsAndDispatch.spec.ts` to cover dispatcher board interactions, override dialog, and assignment confirmation toast.

### Implementation – US2

- [ ] T023 [P] [US2] Implement `DispatchOptimizationService` + helper scoring components in `src/GridPulse.Application/Services/DispatchOptimizationService.cs`, persisting `DispatchRecommendation` rows via repository.
- [ ] T024 [US2] Build `/api/dispatch/recommendations` + `/api/dispatch/assignments` groups in `src/GridPulse.WebApi/Endpoints/DispatchEndpointGroup.cs`, ensuring ProblemDetails for stale telemetry + override justification.
- [ ] T025 [P] [US2] Implement assignment persistence + workload recalculation in `src/GridPulse.Application/Services/DispatchAssignmentService.cs`, updating crew utilization + `AssignmentEvent` history.
- [ ] T026 [P] [US2] Extend `GridPulseApiClient` typed methods for `GetRecommendationsAsync` and `PublishAssignmentAsync` inside `src/GridPulse.Web/Services/GridPulseApiClientExtensions.cs`.
- [ ] T027 [US2] Build `DispatchBoard.razor` UI (Radzen grids/cards) to display recommendations, override prompts, telemetry freshness alerts, and assignment publishing workflow.
- [ ] T028 [US2] Persist override rationale + scoring snapshots in `src/GridPulse.Infrastructure/Repositories/EfDispatchRepository.cs` and surface them via DTOs for audit timelines.

**Checkpoint**: Dispatch workspace independently testable once telemetry + operator features are in place.

---

## Phase 5: User Story 3 – Field Crew Receives Real-Time Updates (Priority: P3)

**Goal**: Crew delivery placeholders push assignments and accept acknowledgements/status updates that flow back into tickets and dispatcher UI.

**Independent Test**: Using the crew simulator or REST calls, assignments reach crews within SLA, acknowledgements return via `/api/crews/{id}/status`, and operator timeline reflects On Scene/Completed events.

### Tests – US3 (write before implementation)

- [ ] T029 [P] [US3] Add unit tests for `CrewAssignmentDeliveryService` covering enqueue/retry logic in `src/GridPulse.Tests.Unit/Application/CrewAssignmentDeliveryServiceTests.cs`.
- [ ] T030 [P] [US3] Add integration tests for `POST /api/crews/{id}/status` ensuring ProblemDetails for invalid transitions in `src/GridPulse.Tests.Unit/WebApi/CrewStatusEndpointTests.cs`.
- [ ] T031 [P] [US3] Add Playwright/bUnit coverage for crew acknowledgement controls surfaced in dispatcher UI (e.g., `CrewStatusPanel.razor`) under `tests/GridPulse.Web.Tests.Playwright/TicketsAndDispatch.spec.ts` or new component tests.

### Implementation – US3

- [ ] T032 [P] [US3] Implement `CrewAssignmentDeliveryService` + persistence of outbound payloads in `src/GridPulse.Application/Services/CrewAssignmentDeliveryService.cs` and call it from assignment pipeline.
- [ ] T033 [US3] Add `/api/crews/{crewId}/status` Minimal API group (new `CrewEndpointGroup.cs`) plus validation + ProblemDetails responses inside `src/GridPulse.WebApi/Endpoints`.
- [ ] T034 [P] [US3] Extend typed client + DTOs for `PostCrewStatusAsync` in `src/GridPulse.Web/Services/GridPulseApiClientExtensions.cs` and surface ack helpers for testing harness.
- [ ] T035 [US3] Update dispatcher UI (`DispatchBoard.razor`, `CrewStatusPanel.razor`) to stream crew status updates, escalate when acknowledgements exceed SLA, and show timeline badges.
- [ ] T036 [US3] Wire crew status events back into ticket workflow (auto transition to `Resolved`, push notifications) within `src/GridPulse.Application/Services/TicketWriteService.cs` + `AssignmentEvent` logging.

**Checkpoint**: All clients (operator, dispatcher, crew placeholder) synchronized with full lifecycle telemetry.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final hardening, docs, and verification across the entire feature.

- [ ] T037 [P] Update documentation (`docs/api.md`, `docs/ui-components.md`, `specs/001-ticket-dispatch/quickstart.md`) with new endpoints, UI flows, and telemetry notes.
- [ ] T038 [P] Regenerate and publish OpenAPI + Scalar docs to include new schemas/endpoints by integrating `specs/001-ticket-dispatch/contracts/openapi.yaml` into `GridPulse.WebApi`.
- [ ] T039 [P] Run full regression suite: `dotnet test GridPulse.Tests.Unit`, `tests/GridPulse.Web.Tests.Playwright`, and `aspire run` smoke per quickstart to ensure E2E readiness.
- [ ] T040 Final observability + resilience review: confirm ServiceDefaults meters, health endpoints, and dashboards capture ticketing/dispatch KPIs before hand-off.

---

## Dependencies & Execution Order

- **Setup (Phase 1)** → **Foundational (Phase 2)** → User Stories (Phases 3–5) → **Polish (Phase 6)**.
- User stories depend on foundational domain/entities/migrations being complete (T005–T010).
- User Story 2 depends on ticket workflow from User Story 1 being testable, but dispatcher work can start once ticket APIs exist.
- User Story 3 depends on User Story 2’s assignment publishing pipeline, yet most delivery service code (T032) can be stubbed once dispatch DTOs exist.
- Tests for each story (T011–T013, T020–T022, T029–T031) should be authored before their corresponding implementation tasks.

### Story Completion Order

1. **US1 (P1)** – MVP baseline.
2. **US2 (P2)** – Builds on tickets.
3. **US3 (P3)** – Requires dispatch assignments.
4. **Polish** – After desired stories ship.

---

## Parallel Execution Examples

- **User Story 1**: T011 (unit tests), T012 (API tests), and T013 (bUnit) can run concurrently; T014 (services) and T017 (typed client) can proceed in parallel once abstractions exist.
- **User Story 2**: T020–T022 tests parallelize, while T023 (scoring) and T026 (typed client) can be built alongside T027 (UI) if API contracts are mocked.
- **User Story 3**: T029–T031 run simultaneously; T032 (delivery service) and T034 (typed client) can execute while T033 (API) is scaffolded since they share DTOs.

---

## Implementation Strategy

1. **MVP First**: Deliver US1 (Phases 1–3) to unlock operator ticketing and validate workflows end-to-end.
2. **Incremental Delivery**: Layer US2 and US3 sequentially, ensuring each passes its independent tests and Playwright flows before merging.
3. **Parallel Teams**: After Phase 2, separate squads can tackle US1/US2/US3 concurrently using the parallel examples above—coordinate via typed client contracts and DTOs.
4. **Hardening**: Finish with Phase 6 polish (docs, telemetry, regression) before release or Aspire publish.
