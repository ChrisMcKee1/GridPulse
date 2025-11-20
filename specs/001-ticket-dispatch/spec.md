# Feature Specification: Ticket Management & Dispatch Optimization

**Feature Branch**: `001-ticket-dispatch`  
**Created**: 2025-11-20  
**Status**: Draft  
**Input**: Product Requirements Document `#file:ZavaPower_PRD.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Operator Manages Outage Tickets (Priority: P1)

Outage operators need to see incoming outage alerts, confirm or edit auto-generated service tickets, assign priorities, and monitor ticket status through its lifecycle.

**Why this priority**: Ticket accuracy and timeliness drive all downstream dispatch actions; without reliable ticket creation and tracking, optimization cannot start.

**Independent Test**: From a seeded outage feed, verify that tickets automatically appear with correct metadata, allow manual edits, and progress through Open → In Progress → Resolved → Closed with audit history.

**Acceptance Scenarios**:

1. **Given** an outage alert with location and severity, **When** ingestion rules meet ticketing criteria, **Then** the system auto-creates a ticket with default priority, links to the outage, and notifies the operator queue.
2. **Given** a ticket in Open state, **When** an operator edits details and promotes it to In Progress, **Then** the system records the edit, re-evaluates dispatch readiness, and timestamps the status change.
3. **Given** a ticket marked Resolved by a crew, **When** an operator verifies the restoration, **Then** the ticket transitions to Closed and becomes read-only.

---

### User Story 2 - Dispatcher Optimizes Crew Assignment (Priority: P2)

Dispatch coordinators need a view of active tickets, crew availability, and recommended assignments ranked by distance, skills, and workload, with the ability to override suggestions.

**Why this priority**: Optimized dispatch is the second lever for resolution time; it depends on P1 ticket data but is otherwise downstream.

**Independent Test**: With simulated crew GPS feeds, verify that the dispatcher UI presents ranked recommendations, records manual overrides, and produces a dispatch order for the selected crew.

**Acceptance Scenarios**:

1. **Given** multiple crews with known coordinates and skills, **When** a dispatcher selects a ticket needing a specific skill, **Then** the system ranks crews by travel ETA + skill match and highlights the top recommendation.
2. **Given** a recommended assignment, **When** the dispatcher overrides it, **Then** the system captures the rationale, updates workload projections, and still generates directions for the chosen crew.
3. **Given** a crew already en route, **When** ticket priority escalates, **Then** dispatch receives a prompt to re-evaluate assignments before confirming the change.

---

### User Story 3 - Field Crew Receives Real-Time Updates (Priority: P3)

Field technicians require mobile-friendly notifications containing ticket context, navigation prompts, and the ability to acknowledge, pause, or mark work complete so operators stay synchronized.

**Why this priority**: Crew experience closes the loop and feeds resolution data back to operations; it depends on the preceding stories for ticket + dispatch data.

**Independent Test**: Using the existing crew mobile surface (or a stub), ensure crews receive push/poll updates within defined SLA, can acknowledge assignments, and that their responses flow back into ticket history.

**Acceptance Scenarios**:

1. **Given** a crew assigned to a ticket, **When** dispatch publishes the assignment, **Then** the crew device receives the job package (location, priority, contact info) within 30 seconds.
2. **Given** a crew en route, **When** they update status to On Scene, **Then** the ticket timeline records the update and notifies the operator.
3. **Given** a crew marks work complete in the app, **When** the event posts back, **Then** the ticket transitions to Resolved and awaits operator verification.

### Edge Cases

- Duplicate outage alerts for the same feeder/transformer must merge into one ticket or mark existing tickets for review to avoid double dispatch.
- Operators need a fallback when GPS/crew telemetry is stale (>5 minutes); dispatcher UI should flag uncertainty and prevent auto-assignment.
- Ticket workflow must handle cancellations (false alarms) without breaking reporting; closed-cancelled tickets still need audit history.
- Field crew devices that fail to acknowledge within SLA trigger escalation notifications to dispatch.

### Constitution Alignment Checklist

- **API Contracts**: `/api/tickets`, `/api/dispatch/assignments`, and `/api/crews/{id}/status` endpoints will expose DTOs consumed by `GridPulseApiClient`; Scalar/OpenAPI must reflect new models and ProblemDetails responses for validation errors.
- **ServiceDefaults & Telemetry**: Ticket + dispatch services emit structured logs and traces for auto-creation, recommendation scoring, and assignment pushes; health checks for GPS ingestion and ticket automation are added via `GridPulse.ServiceDefaults`.
- **Layer Boundaries**: Domain models (Ticket, DispatchRecommendation) live in `GridPulse.Domain`; application services orchestrate repositories; `GridPulse.Web` continues to depend only on DTOs provided by the typed API client; UI leverages Radzen components per UI constitution.
- **Testing**: Unit coverage for ticket state machine, dispatch ranking, and DTO mappers; contract tests for new APIs; bUnit or Playwright coverage for the operator and dispatcher UI surfaces; simulated crew mobile responses for integration testing.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST auto-generate outage tickets when incoming outage reports meet configured thresholds (location, severity, affected customers).
- **FR-002**: Operators MUST be able to create, edit, and delete (prior to dispatch) tickets manually, including priority, affected assets, and notes.
- **FR-003**: Ticket workflow MUST enforce the states Open → In Progress → Resolved → Closed with audit timestamps and actor attribution.
- **FR-004**: The dispatcher workspace MUST display active tickets, crew availability, GPS-derived locations, and recommendation rankings refreshed at least every 60 seconds.
- **FR-005**: Dispatch logic MUST consider crew proximity, skill certifications, workload, and ticket priority when producing ranked assignments.
- **FR-006**: Dispatchers MUST be able to override recommendations, provide justification, and have the system recalculate downstream workload projections.
- **FR-007**: The platform MUST push assignment updates, navigation details, and status prompts to crew mobile clients with acknowledgement tracking.
- **FR-008**: Crew status updates MUST flow back into ticket timelines within 30 seconds of submission, updating operator and dispatcher views in real time.
- **FR-009**: Role-based access control MUST ensure only operators/dispatchers manage tickets/assignments while crews only see their assigned work.
- **FR-010**: The system MUST detect duplicate outage tickets referencing the same asset within 5 minutes and prompt operators to merge or confirm distinct incidents.
- **FR-011**: The solution MUST expose reporting hooks (API or export) for ticket SLA metrics (time to dispatch, time to resolve) to feed downstream analytics.
- **FR-012**: Crew mobile delivery mechanism MUST expose a stable contract and placeholder integration surface so the future field app can consume assignments; for this release, provide mock delivery endpoints plus UI hooks that mirror the expected payload.
- **FR-013**: GPS/telemetry ingestion MUST rely on simulated data providers (mock feeds refreshed every 60 seconds) with scaffolding to swap in the eventual production API without reshaping downstream consumers.

### Key Entities *(include if feature involves data)*

- **Ticket**: Represents an outage work item with attributes such as outage reference, priority, status, timestamps, assigned crew, and audit log.
- **DispatchRecommendation**: Captures ranked pairing between a ticket and a crew, including score breakdown (distance, skills, workload) and dispatcher override notes.
- **Crew**: Field team profile containing skills/certifications, current workload, and latest location snapshot reference.
- **CrewLocationSnapshot**: Time-stamped latitude/longitude plus data quality indicators used by dispatch ranking.
- **AssignmentEvent**: Timeline entry recording dispatch decisions, crew acknowledgements, arrival/on-scene, completion, or cancellation signals.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 90% of qualifying outage reports auto-create tickets without operator edits, and every ticket forms within 2 seconds of input arrival.
- **SC-002**: Dispatch recommendations appear within 5 seconds of ticket readiness and reflect accurate GPS data (>95% within 100 meters) when telemetry is available.
- **SC-003**: Crew assignment acknowledgements reach the platform within 30 seconds in 95% of cases, and ticket timelines reflect those updates instantly.
- **SC-004**: Average outage resolution time for dispatched tickets decreases by at least 30% compared to the baseline documented before rollout.
- **SC-005**: Operators/dispatchers report ≥4/5 satisfaction in post-release surveys regarding ticket visibility and dispatch controls.

## Assumptions & Dependencies

- Existing outage ingestion pipeline can surface severity/location data required for auto-ticketing.
- Field crew mobile experience is planned for a future release; this iteration provides placeholder APIs/UI hooks plus mock payload validators so the eventual app can integrate with minimal rework.
- GPS data sources are simulated mock feeds refreshed every 60 seconds; production integration will plug into the same interface when the upstream provider is ready.
- Role/identity provider updates (e.g., Entra) must land before enforcing RBAC in UI/API.
- Until Entra authentication is installed, the app operates with an auto-approved default identity so security code must remain swappable for real claims later.
