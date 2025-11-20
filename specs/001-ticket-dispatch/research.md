# Research: Ticket Management & Dispatch Optimization

**Date**: 2025-11-20  
**Participants**: GridPulse engineering (application, infrastructure, web), product  
**Inputs**: PRD, system architecture docs, GridPulse constitution, existing outage sample data

---

## Decision 1 – Ticket Automation Pipeline

- **Decision**: Implement `TicketAutomationService` in the application layer backed by an EF Core repository and hosted worker (`TicketAutomationWorker`) inside `GridPulse.WebApi`. The worker subscribes to the existing outage ingestion feed (currently seeded JSON) and creates/updates tickets via the service.
- **Rationale**: Keeps automation logic reusable (unit-testable) and prevents UI/API projects from owning state transitions. Hosted worker lives alongside Minimal APIs so Aspire orchestrates it automatically.
- **Alternatives Considered**:
  - *API-only creation*: Rejected because it forces operators to manually submit data and defeats automation goals.
  - *Database triggers*: Rejected to preserve Clean Architecture and maintain portability to Azure Database for PostgreSQL.

## Decision 2 – Dispatch Optimization Algorithm

- **Decision**: Score each crew using a weighted formula: `score = w_distance * normalizedETA + w_skill * skillPenalty + w_workload * utilization`. Initial weights: distance 0.5, skill 0.3, workload 0.2. Service normalizes scores per ticket and returns ranked recommendations.
- **Rationale**: Deterministic, explainable ranking fits MVP needs and keeps infrastructure simple. We can adjust weights via configuration without redeploying code.
- **Alternatives Considered**:
  - *Heuristic rules-only (distance first, fallback skill)*: Lacked flexibility and produced poor outcomes in simulated data.
  - *ML-based optimization*: Overkill for current dataset; requires labelled history we do not yet have.

## Decision 3 – Mock Telemetry Feed Architecture

- **Decision**: Introduce `ICrewTelemetryFeed` abstraction with a default `MockCrewTelemetryFeed` implementation that replays scripted GPS points every 60 seconds via `IHostedService`. Telemetry writes the latest snapshot to PostgreSQL for auditing while keeping an in-memory cache for fast reads.
- **Rationale**: Maintains contract needed by dispatch service today while making it trivial to swap the implementation once the real telemetry API exists. Persisting snapshots allows troubleshooting and aligns with observability principles.
- **Alternatives Considered**:
  - *Pure in-memory mock*: No audit trail or replay ability.
  - *External simulator*: Adds infrastructure overhead without additional value for this phase.

## Decision 4 – Placeholder Crew Delivery Interface

- **Decision**: Expose `POST /api/crews/{id}/status` plus a SignalR-free polling endpoint consumed by the Blazor UI to demonstrate assignment delivery. Internally, produce a `CrewAssignmentDeliveryService` that logs payloads and stores them in PostgreSQL for future mobile retrieval.
- **Rationale**: Keeps contracts stable for the forthcoming crew mobile app and lets us surface assignment timelines in the UI today. Avoids premature investment in push infrastructure.
- **Alternatives Considered**:
  - *Implement full mobile client now*: Out of scope; no product design yet.
  - *Skip delivery placeholders*: Would leave no contract to build against later.

## Decision 5 – Data & Migration Strategy

- **Decision**: Single EF migration adding `Tickets`, `TicketEvents`, `Crew`, `CrewLocationSnapshots`, and `DispatchAssignments` tables, plus seed data for sample tickets/crews. Use PostgreSQL sequences and JSONB columns for audit metadata. Provide `SeedData` utility to reset demo data during `aspire run`.
- **Rationale**: Aligns with clean layering, keeps state normalized, and mirrors eventual production schema.
- **Alternatives Considered**:
  - *Separate ticket DB*: Unnecessary complexity for MVP and complicates transactions.
  - *Denormalized ticket table*: Would make auditing workflow transitions harder and violate reporting requirements.
