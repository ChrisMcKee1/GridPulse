# Quickstart – Ticket Management & Dispatch Optimization

Use this runbook to validate the ticketing + dispatch experience end-to-end inside an Aspire dev environment.

## 1. Prerequisites

- .NET 9 SDK w/ Aspire 13 CLI installed (`aspire --version`).
- PostgreSQL container provisioned by `GridPulse.AppHost` (handled automatically when using Aspire run).
- Radzen components already referenced by `GridPulse.Web`; ensure `npm install` has been executed for Playwright assets if UI tests will run.
- Feature branch `001-ticket-dispatch` checked out with migrations applied.
- Temporary authentication shim enabled: the app boots with a mock operator/dispatcher identity via `IUserContext`. No Entra setup is required yet, but do not remove the abstraction so production auth can drop in later.

## 2. Database Prep

1. From repo root, run migrations against the Aspire Postgres instance:

   ```powershell
   dotnet ef database update --project src/GridPulse.Infrastructure/GridPulse.Infrastructure.csproj --startup-project src/GridPulse.AppHost/GridPulse.AppHost.csproj
   ```

2. Seed crews + telemetry samples:

   ```powershell
   dotnet run --project src/GridPulse.AppHost/GridPulse.AppHost.csproj -- seed-data ticketing
   ```

   - Seed routine inserts sample crews (10) with skill coverage, base tickets, and kicks off the mock telemetry feed (refreshes every 60s).

## 3. Run the Aspire stack


```powershell
aspire run --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj
```


- API available at `https://localhost:7143` (Scalar docs under `/scalar/v1`).
- Blazor UI served from `https://localhost:7210` (actual port shown in CLI output via `WithReference(api)`).
- Ensure ServiceDefaults dashboard displays new meters: `ticketing.automation`, `dispatch.recommendation`, `crew.delivery`.
- Confirm the header badge shows the mock signed-in persona ("Auto Operator"); this verifies the placeholder identity is wired before real Entra integration.
- When debugging the API outside Aspire (`dotnet run --project src/GridPulse.WebApi/GridPulse.WebApi.csproj --launch-profile https`), the browser now opens Scalar automatically because `launchSettings.json` sets `launchUrl` to `/scalar/v1`.

## 4. Operator Flow (UI)

1. Navigate to **Tickets** page (`GridPulse.Web` > `Tickets.razor`).
2. Confirm auto-generated tickets appear (Open state) from seed data.
3. Use "Create Ticket" Radzen dialog to submit a manual ticket; verify data validation (priority required, assets <=20).
4. Select a ticket and click **Promote to In Progress**. UI should call `UpdateTicketStatusAsync` and refresh summary pane.
5. Review Timeline component (AssignmentEvent log) for audit entries.

## 5. Dispatcher Flow (UI)

1. Open **Dispatch Board** page.
2. Select an `In Progress` ticket → verify recommendations load within 5 seconds.
3. Accept the top recommendation; ensure override dialog appears when picking a non-top crew.
4. Confirm assignment receipt toast uses `AssignmentReceiptDto.trackingId` and the ticket row shows assigned crew + ETA.

## 6. Crew Simulation

1. Use either the **Crew Status** panel on the Dispatch Board (preferred for demo) or REST client to POST to `/api/crews/{crewId}/status` with payloads from `contracts/openapi.yaml`.
2. Post `{ "status": "acknowledged" }` and confirm dispatcher board timeline + crew badge update.
3. Post `{ "status": "completed" }` to trigger ticket transition to `Resolved`; operator must close to finalize.

## 7. Testing Checklist

- `dotnet test src/GridPulse.Tests.Unit/GridPulse.Tests.Unit.csproj`
- `dotnet test tests/GridPulse.Web.Tests.Playwright/GridPulse.Web.Tests.Playwright.csproj` (ensure `PLAYWRIGHT_BROWSERS_PATH=0` if running locally).
- Optional: `aspire run --project ... --filter TicketingSmoke` once a smoke test project exists.

## 8. Troubleshooting

- **Telemetry stale warning**: check `MockCrewTelemetryFeed` logs via Aspire dashboard; restart feed if last heartbeat >2 min.
- **Migrations missing**: ensure `GridPulse.Infrastructure` references include the new ticketing migration file; rerun `dotnet ef migrations add` if local db was reset.
- **UI not calling typed client**: verify DI registration for `GridPulseApiClient` and new extension methods in `GridPulse.Web/Services`.

## 9. Next Steps

- After verifying locally, capture screenshots or Playwright video for PR evidence per UI constitution.
- Update `docs/ui-components.md` and `docs/api.md` with any user-facing changes made while implementing this plan.
- Regenerate `specs/001-ticket-dispatch/contracts/openapi.yaml` after contract changes: `dotnet openapi add GridPulse.WebApi --uri https://localhost:7143/openapi/v1.json --output specs/001-ticket-dispatch/contracts/openapi.yaml` (use `dotnet openapi remove` first if entry exists).
