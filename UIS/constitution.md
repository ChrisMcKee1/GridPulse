# GridPulse UI Constitution (Lite)

> Inherits from the parent [GridPulse Constitution](../.specify/memory/constitution.md); any conflicts are resolved in favor of the parent document.

## UI Principles

1. **Typed Client Only** – All Blazor components consume backend data exclusively through DI-registered typed clients (currently `GridPulseApiClient`). Direct `HttpClient` instantiation or EF references are prohibited.
2. **Render Mode Discipline** – The UI remains in Blazor Interactive Auto. Server-side prerender + WASM handoff must stay seamless; new features document their hydration impact and Radzen component usage.
3. **DTO-Only State** – UI state derives from application-layer DTOs. Components must not rely on persistence entities or infrastructure-specific models.
4. **Telemetry & Resilience Hooks** – UI service registrations go through `GridPulse.ServiceDefaults` so HttpClient resilience, logging, and distributed tracing remain consistent.
5. **Accessibility & Responsiveness** – Radzen or custom components must meet WCAG 2.1 AA equivalents (keyboard nav, ARIA labels) and scale for desktop/tablet breakpoints.
6. **Radzen-First UI** – Radzen is the default component library. Exhaust Radzen (or another approved framework) before building custom components; any bespoke component requires documented justification plus reuse plan.

## Workflow Expectations

- Feature specs identify which pages/components change, the DTOs involved, and any new typed client calls.
- Plans/tasks must allocate work for telemetry (loading/error states), tests (bUnit/Playwright when applicable), and documentation updates in `docs/ui-components.md`.
- Before implementing or upgrading Radzen components, engineers validate the component’s props/methods against current Radzen documentation to avoid stale APIs.
- PRs include visual verification (screenshots or Playwright recordings) and reference this constitution in the checklist.

## Governance

- Violations escalate to the parent constitution’s compliance process.
- Amendments to this lite constitution require updating both this file and referencing text inside the parent constitution.
- Last reviewed whenever the parent constitution version changes; additional interim reviews happen when Blazor architecture shifts.
