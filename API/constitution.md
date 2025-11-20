# GridPulse API Constitution (Lite)

> Inherits from the parent [GridPulse Constitution](../.specify/memory/constitution.md); any conflicts fall back to the parent document.

## API Principles

1. **Minimal API Discipline** – Group endpoints under `/api/...` with `MapGroup`, `WithTags`, and typed DI parameters. Route builders must stay in `GridPulse.WebApi` without leaking anonymous lambda logic into other projects.
2. **ProblemDetails Everywhere** – All failures return RFC 7807 payloads via `AddProblemDetails()`; do not craft ad-hoc JSON or plaintext errors.
3. **Repository Isolation** – API endpoints delegate to application services/repositories only. They never talk to `GridPulseDbContext` or infrastructure implementations directly.
4. **Observability via ServiceDefaults** – Instrument request/response logging, tracing, and health endpoints through `GridPulse.ServiceDefaults`. Any new telemetry exports start there before per-endpoint customization.
5. **Testing & Contracts** – New endpoints require contract/integration coverage plus OpenAPI + Scalar documentation updates in the same PR.

## Workflow Expectations

- Feature specs list precise routes, verbs, DTOs, and expected status codes.
- Plans/tasks must budget for migrations/seed data, telemetry additions, and contract tests in `GridPulse.Tests.Unit` or future integration suites.
- PRs link to generated Scalar docs (`/scalar/v1`) or attach OpenAPI diffs proving the contract change is intentional.

## Governance

- Non-compliant contributions are blocked until they match this lite constitution and the parent document.
- Amendments follow the same RFC + parent update cycle as the main constitution.
- Review cadence matches the parent constitution or any major API surface change, whichever comes first.
