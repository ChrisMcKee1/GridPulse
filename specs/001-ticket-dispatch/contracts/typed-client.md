# Typed Client Surface – GridPulseApiClient additions

All UI calls must flow through the DI-registered `GridPulseApiClient`. The following helpers will be added inside `GridPulse.Web/Services/GridPulseApiClientExtensions.cs` so the UI complies with `UIS/constitution.md`.

| Method | HTTP | Route | Notes |
|--------|------|-------|-------|
| `Task<TicketDto> CreateTicketAsync(TicketCreateRequest body, CancellationToken ct)` | `POST` | `/api/tickets` | Used by operator Radzen form for manual tickets. |
| `Task<PagedResult<TicketDto>> GetTicketsAsync(TicketFilter filter, CancellationToken ct)` | `GET` | `/api/tickets` | Supports status, minPriority, `includeRecommendations`. |
| `Task<TicketDto> UpdateTicketStatusAsync(Guid ticketId, TicketStatusUpdateRequest body, CancellationToken ct)` | `PATCH` | `/api/tickets/{ticketId}/status` | Enforces workflow guardrails; returns updated DTO for UI state sync. |
| `Task<DispatchRecommendationsEnvelope> GetRecommendationsAsync(Guid ticketId, CancellationToken ct)` | `GET` | `/api/dispatch/recommendations?ticketId=` | Dispatcher board polls every 30s while in focus. |
| `Task<AssignmentReceiptDto> PublishAssignmentAsync(DispatchAssignmentRequest body, CancellationToken ct)` | `POST` | `/api/dispatch/assignments` | Fires when dispatcher accepts/overrides recommended crew. |
| `Task<CrewStatusUpdateResponse> PostCrewStatusAsync(Guid crewId, CrewStatusUpdateRequest body, CancellationToken ct)` | `POST` | `/api/crews/{crewId}/status` | Placeholder used by QA harness + eventual mobile client; UI may trigger via mock controls to simulate acknowledgements. |

DTO namespace references:

- Requests/Responses reuse the schemas defined in `contracts/openapi.yaml`.
- `TicketDto`, `DispatchRecommendationDto`, and `CrewStatusDto` reside in `GridPulse.Application.Models` to keep the UI persistence-agnostic.
