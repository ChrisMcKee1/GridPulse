# GridPulse UI Constitution (Lite)

> Inherits from the parent [GridPulse Constitution](../.specify/memory/constitution.md); any conflicts are resolved in favor of the parent document.

## UI Principles

1. **Typed Client Only** – All Blazor components consume backend data exclusively through DI-registered typed clients (currently `GridPulseApiClient`). Direct `HttpClient` instantiation or EF references are prohibited.
2. **Render Mode Discipline** – Pages with complex state or interactions use `@rendermode InteractiveServer`. Use `InteractiveAuto` sparingly and only when server-side prerender + WASM handoff is explicitly tested for Radzen component compatibility. New features document their render mode choice and hydration impact.
3. **DTO-Only State** – UI state derives from application-layer DTOs. Components must not rely on persistence entities or infrastructure-specific models.
4. **Telemetry & Resilience Hooks** – UI service registrations go through `GridPulse.ServiceDefaults` so HttpClient resilience, logging, and distributed tracing remain consistent.
5. **Accessibility & Responsiveness** – Radzen or custom components must meet WCAG 2.1 AA equivalents (keyboard nav, ARIA labels) and scale for desktop/tablet breakpoints.
6. **Radzen-First UI** – Radzen is the default component library. **NEVER use raw HTML elements** (`<h1>`, `<p>`, `<span>`, `<div>`, `<label>`, `<ul>`, `<li>`) in Razor components—always use Radzen equivalents (`RadzenText`, `RadzenLabel`, `RadzenBadge`, `RadzenStack`, `RadzenRow`, `RadzenColumn`, etc.). Exhaust Radzen before building custom components; any bespoke component requires documented justification plus reuse plan.
7. **Component Hierarchy** – Use proper Radzen layout components:
   - `RadzenStack` for vertical/horizontal stacking (replaces `<div>` containers)
   - `RadzenRow`/`RadzenColumn` for responsive grids (replaces CSS grid/flexbox)
   - `RadzenSplitter`/`RadzenSplitterPane` for resizable panels (replaces manual layout)
   - `RadzenCard` for content containers (replaces `<div class="card">`)
   - `DialogService.OpenAsync()` for modals (replaces custom modal HTML)
8. **Typography** – All text must use `RadzenText` with appropriate `TextStyle` and `TagName`:
   - Headings: `TextStyle.H1`–`H6` with matching `TagName`
   - Body: `TextStyle.Body1` or `Body2`
   - Captions: `TextStyle.Caption`
   - Subtitles: `TextStyle.Subtitle1` or `Subtitle2`
9. **Form Validation** – Use `<RadzenTemplateForm>` with `<DataAnnotationsValidator />` and `<ValidationMessage For="@(() => model.Property)" />` per field—never use `<ValidationSummary />` alone.

## Workflow Expectations

- Feature specs identify which pages/components change, the DTOs involved, any new typed client calls, and the chosen render mode with justification.
- Plans/tasks must allocate work for telemetry (loading/error states), tests (bUnit/Playwright when applicable), and documentation updates in `docs/ui-components.md`.
- Before implementing or upgrading Radzen components, engineers validate the component's props/methods against current Radzen documentation to avoid stale APIs.
- **Pre-implementation checklist**: Verify all HTML elements are replaced with Radzen equivalents, confirm render mode choice, validate form structure uses `RadzenTemplateForm`, ensure layout uses `RadzenStack`/`RadzenRow`/`RadzenColumn`.
- PRs include visual verification (screenshots or Playwright recordings), render mode confirmation, and reference this constitution in the checklist.

## Governance

- Violations escalate to the parent constitution’s compliance process.
- Amendments to this lite constitution require updating both this file and referencing text inside the parent constitution.
- Last reviewed whenever the parent constitution version changes; additional interim reviews happen when Blazor architecture shifts.
