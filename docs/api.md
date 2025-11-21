# API Documentation

Complete reference for the GridPulse REST API endpoints.

## Table of Contents
- [Overview](#overview)
- [Base URL](#base-url)
- [Authentication](#authentication)
- [Endpoints](#endpoints)
- [Data Models](#data-models)
- [Error Handling](#error-handling)
- [Interactive Documentation](#interactive-documentation)

---

## Overview

The GridPulse API is a RESTful HTTP API built with .NET Minimal APIs. It provides endpoints for querying power outage information and will expand to support customer management, usage tracking, and notifications.

### API Characteristics
- **Architecture**: REST with JSON
- **Framework**: ASP.NET Core Minimal APIs
- **Documentation**: OpenAPI 3.0 with Scalar
- **Serialization**: System.Text.Json with enum name conversion
- **Error Format**: RFC 7807 Problem Details

---

## Base URL

### Development
```
https://localhost:7143
```

### Production
```
TBD - Will be deployed to Azure
```

---

## Authentication

**Current Status**: ❌ Not Implemented

**Planned**: Microsoft Entra (B2C for customers, Entra ID for operators)

All endpoints are currently **publicly accessible** for development purposes.

---

## Endpoints

### Health Check

Check API service health status.

#### Request
```http
GET /api/health
```

#### Response
**Status**: 200 OK

```json
{
  "status": "healthy"
}
```

#### Purpose
- Verify API availability
- Load balancer health checks
- Monitoring and alerting
- Aspire dashboard integration

---

### List Recent Outages

Retrieve the most recent power outages.

#### Request
```http
GET /api/outages
```

#### Query Parameters
None (currently returns 25 most recent outages)

#### Response
**Status**: 200 OK

```json
[
  {
    "id": "17aa5a0f-4f5f-45ec-8dc0-1b53e876c111",
    "serviceLocationId": "f0b8c3a0-4f85-4f50-9b26-7b6b4c1cf001",
    "serviceAddress": "123 Contoso Ave, Apex, NC",
    "status": "CrewDispatched",
    "reportedAt": "2025-01-19T13:04:22Z",
    "lastUpdatedAt": "2025-01-19T14:44:22Z",
    "estimatedRestoration": "2025-01-19T16:04:22Z",
    "cause": "Tree on line",
    "affectedCustomers": 1
  }
]
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | `string` (GUID) | Unique outage identifier |
| `serviceLocationId` | `string` (GUID) | Associated service location |
| `serviceAddress` | `string` | Human-readable address |
| `status` | `string` (enum) | Current status (Reported, Acknowledged, CrewDispatched, Restored) |
| `reportedAt` | `string` (ISO 8601) | When outage was first reported |
| `lastUpdatedAt` | `string` (ISO 8601) | Most recent update timestamp |
| `estimatedRestoration` | `string` (ISO 8601) or `null` | ETA for restoration |
| `cause` | `string` or `null` | Root cause description |
| `affectedCustomers` | `number` | Count of events/customers affected |

#### Ordering
- Results are ordered by `reportedAt` (most recent first)
- Maximum 25 results returned

#### Example cURL
```bash
curl -X GET https://localhost:7143/api/outages \
  -H "Accept: application/json"
```

#### Example Response
```json
[
  {
    "id": "17aa5a0f-4f5f-45ec-8dc0-1b53e876c111",
    "serviceLocationId": "f0b8c3a0-4f85-4f50-9b26-7b6b4c1cf001",
    "serviceAddress": "123 Contoso Ave, Apex, NC",
    "status": "CrewDispatched",
    "reportedAt": "2025-01-19T13:04:22.123Z",
    "lastUpdatedAt": "2025-01-19T14:44:22.456Z",
    "estimatedRestoration": "2025-01-19T16:04:22.789Z",
    "cause": "Tree on line",
    "affectedCustomers": 1
  }
]
```

---

### Get Outage by ID

Retrieve details for a specific outage.

#### Request
```http
GET /api/outages/{id}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Outage identifier |

#### Response

**Status**: 200 OK (outage found)

```json
{
  "id": "17aa5a0f-4f5f-45ec-8dc0-1b53e876c111",
  "serviceLocationId": "f0b8c3a0-4f85-4f50-9b26-7b6b4c1cf001",
  "serviceAddress": "123 Contoso Ave, Apex, NC",
  "status": "CrewDispatched",
  "reportedAt": "2025-01-19T13:04:22Z",
  "lastUpdatedAt": "2025-01-19T14:44:22Z",
  "estimatedRestoration": "2025-01-19T16:04:22Z",
  "cause": "Tree on line",
  "affectedCustomers": 1
}
```

**Status**: 404 Not Found (outage not found)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404
}
```

#### Example cURL
```bash
curl -X GET https://localhost:7143/api/outages/17aa5a0f-4f5f-45ec-8dc0-1b53e876c111 \
  -H "Accept: application/json"
```

---

### Ticket Management

Ticket endpoints power the operator workspace (`src/GridPulse.Web/GridPulse.Web/Components/Pages/Tickets.razor`). All handlers return DTOs defined in `GridPulse.Application.Models` and follow ProblemDetails for validation failures.

#### Create Ticket

```http
POST /api/tickets
```

##### Ticket Request Body

```json
{
  "title": "Transformer fire near Elm",
  "outageReferenceId": "OUT-1099",
  "description": "Reported by AMI sensor",
  "priority": "High",
  "customerImpact": 75,
  "affectedAssets": ["TX-442", "CB-10"]
}
```

##### Ticket Responses

- `201 Created` → `TicketDto` payload for the new ticket
- `400 Bad Request` → Validation error (missing title, >20 assets, duplicated outage reference)
- `409 Conflict` → Duplicate open ticket detected via outage reference

#### Query Tickets

```http
GET /api/tickets?statuses=Open,InProgress&minPriority=High&includeTimeline=true&includeRecommendations=true
```

##### Ticket Query Parameters

| Name | Type | Description |
|------|------|-------------|
| `statuses` | CSV of `TicketStatus` | Optional filter (defaults to Open + InProgress) |
| `minPriority` | `TicketPriority` | Minimum severity to return |
| `includeTimeline` | `bool` | Include `AssignmentEventDto` timeline entries |
| `includeRecommendations` | `bool` | Include dispatch recommendations |

##### Ticket Query Response

```json
{
  "items": [
    {
      "id": "a065612f-283c-4577-ab91-c144a92dff2d",
      "title": "Feeder fault – Substation 12",
      "status": "InProgress",
      "priority": "High",
      "customerImpact": 120,
      "etaMinutes": 45,
      "assignedCrewId": "372551b7-cddc-4586-ad40-f430b6cf5352",
      "assignedCrewName": "Crew Delta",
      "affectedAssets": ["FD-12"],
      "openedAt": "2025-11-20T23:01:02.431Z",
      "updatedAt": "2025-11-21T02:31:38.925Z",
      "events": [...],
      "recommendations": [...]
    }
  ]
}
```

#### Update Ticket Status

```http
PATCH /api/tickets/{ticketId}/status
```

##### Ticket Status Payload

```json
{
  "status": "Resolved",
  "reason": "Crew reported completed field work"
}
```

##### Ticket Status Responses

- `200 OK` → Updated `TicketDto`
- `400 Bad Request` → Invalid payload or missing reason for cancellation
- `404 Not Found` → Ticket ID unknown
- `409 Conflict` → Workflow violation (e.g., Closed → InProgress)

---

### Dispatch Operations

Dispatch endpoints feed `DispatchBoard.razor` with recommendation data and post assignments to the automation worker.

#### Get Recommendations

```http
GET /api/dispatch/recommendations?ticketId={guid}
```

##### Recommendation Responses

- `200 OK` → `DispatchRecommendationsEnvelope` containing ticket summary plus ordered crew list
- `400 Bad Request` → Missing `ticketId`
- `404 Not Found` → Ticket not in a dispatchable state

Example payload:

```json
{
  "ticket": {
    "id": "5f3c6d45-d5ea-4b75-9a39-5b7db9a19e96",
    "title": "Pole fire",
    "priority": "High"
  },
  "recommendations": [
    {
      "id": "8f892c9a-5d0b-4b04-92e5-16b2ac9ce836",
      "ticketId": "5f3c6d45-d5ea-4b75-9a39-5b7db9a19e96",
      "crew": {
        "crewId": "9994df1e-52e7-4c21-9114-9ba43a2f9203",
        "displayName": "Crew Bravo",
        "status": "Assigned",
        "currentTicketCount": 1,
        "skills": ["Transformer"],
        "lastStatusUpdate": "2025-11-21T02:15:00Z",
        "location": {
          "latitude": 33.12,
          "longitude": -96.81,
          "capturedAt": "2025-11-21T02:14:30Z",
          "speedMph": 42
        },
        "isTelemetryStale": false
      },
      "compositeScore": 0.92,
      "scoreComponents": {
        "distance": 0.48,
        "skill": 0.30,
        "workload": 0.14
      },
      "etaMinutes": 32,
      "isAutoSelected": true,
      "isOverride": false,
      "createdAt": "2025-11-21T02:16:45.112Z"
    }
  ]
}
```

#### Publish Assignment

```http
POST /api/dispatch/assignments
```

##### Assignment Request Body

```json
{
  "ticketId": "5f3c6d45-d5ea-4b75-9a39-5b7db9a19e96",
  "crewId": "9994df1e-52e7-4c21-9114-9ba43a2f9203",
  "overrideReason": "Crew Alpha diverted for hospital outage"
}
```

##### Assignment Responses

- `202 Accepted` → `AssignmentReceiptDto` (`trackingId`, `deliveryStatus`)
- `400 Bad Request` → Missing identifiers or override details when required
- `404 Not Found` → Ticket/crew not found
- `409 Conflict` → Recommendation expired or telemetry stale

---

### Crew Status Updates

Crew simulators (and eventually the mobile app) call this endpoint to acknowledge assignments or report progress. Updates flow through `DispatchAssignmentService`, update the ticket timeline, and can close tickets automatically when crews report completion.

#### Update Crew Status

```http
POST /api/crews/{crewId}/status
```

##### Crew Status Payload

```json
{
  "status": "acknowledged",
  "note": "En route once refueled",
  "location": {
    "latitude": 33.10,
    "longitude": -96.80,
    "capturedAt": "2025-11-21T02:25:00Z",
    "speedMph": 38
  }
}
```

##### Crew Status Responses

- `202 Accepted` → `CrewStatusUpdateResponse` referencing the affected ticket
- `400 Bad Request` → Missing crew ID or payload
- `404 Not Found` → No active assignment for the crew

---

## Data Models

### OutageSummary

Response model for outage information.

```typescript
interface OutageSummary {
  id: string;                      // GUID
  serviceLocationId: string;       // GUID
  serviceAddress: string;
  status: OutageStatus;
  reportedAt: string;             // ISO 8601 datetime
  lastUpdatedAt: string;          // ISO 8601 datetime
  estimatedRestoration: string | null;  // ISO 8601 datetime
  cause: string | null;
  affectedCustomers: number;
}
```

### OutageStatus (Enum)

```typescript
enum OutageStatus {
  Reported = "Reported",
  Acknowledged = "Acknowledged",
  CrewDispatched = "CrewDispatched",
  Restored = "Restored"
}
```

**Values**:

- `Reported` - Initial state when outage is detected
- `Acknowledged` - Outage confirmed by operator
- `CrewDispatched` - Repair crew en route
- `Restored` - Power fully restored

### TicketDto

```typescript
interface TicketDto {
  id: string;
  title: string;
  outageReferenceId: string;
  priority: TicketPriority;
  status: TicketStatus;
  customerImpact: number;
  etaMinutes: number | null;
  assignedCrewId: string | null;
  assignedCrewName: string | null;
  affectedAssets: string[];
  openedAt: string;
  updatedAt: string;
  closedAt: string | null;
  events: AssignmentEventDto[];
  recommendations: DispatchRecommendationDto[];
}
```

### DispatchRecommendationDto

```typescript
interface DispatchRecommendationDto {
  id: string;
  ticketId: string;
  crew: CrewStatusDto;
  compositeScore: number;
  scoreComponents: Record<string, number>;
  etaMinutes: number;
  isAutoSelected: boolean;
  isOverride: boolean;
  overrideReason: string | null;
  createdAt: string;
  expiresAt: string | null;
}
```

### CrewStatusDto

```typescript
interface CrewStatusDto {
  crewId: string;
  displayName: string;
  status: CrewStatus;
  currentTicketCount: number;
  skills: CrewSkill[];
  lastStatusUpdate: string;
  location?: CrewLocationSnapshot;
  isTelemetryStale: boolean;
}
```

### AssignmentEventDto

```typescript
interface AssignmentEventDto {
  id: string;
  ticketId: string;
  eventType: string;
  createdAt: string;
  createdBy: string;
  note?: string;
}
```

### CrewStatusUpdateRequest

```typescript
interface CrewStatusUpdateRequest {
  status: CrewAssignmentStatus;
  note?: string;
  location?: {
    latitude: number;
    longitude: number;
    capturedAt: string;
    speedMph?: number;
  };
}
```

### AssignmentReceiptDto

```typescript
interface AssignmentReceiptDto {
  ticketId: string;
  crewId: string;
  deliveryStatus: string; // queued, acknowledged, completed
  trackingId: string;
}
```

---

## Error Handling

The API uses RFC 7807 Problem Details for error responses.

### Problem Details Format

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "The request could not be processed"
}
```

### Standard HTTP Status Codes

| Status Code | Description | When Used |
|-------------|-------------|-----------|
| 200 | OK | Successful request |
| 400 | Bad Request | Invalid request format or parameters |
| 404 | Not Found | Resource doesn't exist |
| 500 | Internal Server Error | Unexpected server error |

### Error Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `type` | `string` (URI) | Problem type reference |
| `title` | `string` | Human-readable error title |
| `status` | `number` | HTTP status code |
| `detail` | `string` (optional) | Detailed error description |
| `instance` | `string` (optional) | Specific problem instance |

### Example Error Responses

#### 404 Not Found

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404
}
```

#### 500 Internal Server Error

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500
}
```

---

## CORS Configuration

The API is configured with a permissive CORS policy for development:

- **Allowed Origins**: Any (`*`)
- **Allowed Methods**: Any
- **Allowed Headers**: Any

**Production Note**: This should be restricted to specific origins in production.

---

## Content Negotiation

### Request Headers

```http
Accept: application/json
```

### Response Headers

```http
Content-Type: application/json; charset=utf-8
```

### Serialization Settings

- **Enum Serialization**: String (e.g., `"CrewDispatched"` not `2`)
- **Date Format**: ISO 8601 with timezone (e.g., `"2025-01-19T13:04:22Z"`)
- **Null Handling**: Nullable fields can be `null`
- **Property Naming**: camelCase

---

## Interactive Documentation

### Scalar API Reference

The API provides interactive documentation using Scalar (replacement for Swagger UI).

**URL**: `https://localhost:7143/scalar/v1`

**Availability**: Development environment only

**Features**:

- Interactive request testing
- Schema documentation
- Example requests and responses
- Try-it-out functionality

> **Tip**: Launching `GridPulse.WebApi` with the `https` profile now opens Scalar automatically (`launchUrl` points to `/scalar/v1`).

### OpenAPI Specification

**Runtime URL**: `https://localhost:7143/openapi/v1.json`

**Contract Source**: `specs/001-ticket-dispatch/contracts/openapi.yaml`

Download the OpenAPI specification for use with:

- Code generation tools
- API testing tools (Postman, Insomnia)
- Documentation generators

To refresh the spec file from a running API instance:

```powershell
dotnet openapi remove GridPulse.WebApi
dotnet openapi add GridPulse.WebApi --uri https://localhost:7143/openapi/v1.json --output specs/001-ticket-dispatch/contracts/openapi.yaml
```

---

## Rate Limiting

**Current Status**: ❌ Not Implemented

**Planned**: API rate limiting to prevent abuse

---

## API Versioning

**Current Status**: ❌ Not Implemented

**Planned**: URL-based versioning (e.g., `/api/v1/outages`, `/api/v2/outages`)

---

## Future Endpoints

The following endpoints are planned but not yet implemented:

### Outage Write Operations

```http
POST   /api/outages                    # Create new outage
PUT    /api/outages/{id}               # Update outage
PATCH  /api/outages/{id}/status        # Update status only
DELETE /api/outages/{id}               # Cancel outage
```

### Outage Events

```http
GET    /api/outages/{id}/events        # List outage events
POST   /api/outages/{id}/events        # Add event/note
```

### Customers

```http
GET    /api/customers                  # List customers
GET    /api/customers/{id}             # Get customer
POST   /api/customers                  # Create customer
PUT    /api/customers/{id}             # Update customer
```

### Service Locations

```http
GET    /api/customers/{id}/locations   # List locations for customer
GET    /api/locations/{id}             # Get location details
POST   /api/locations                  # Add service location
```

### Usage Readings

```http
GET    /api/locations/{id}/usage       # Get usage history
GET    /api/locations/{id}/usage/latest # Get latest reading
POST   /api/locations/{id}/usage       # Record new reading
```

### Notifications

```http
GET    /api/customers/{id}/preferences # Get notification preferences
PUT    /api/customers/{id}/preferences # Update preferences
POST   /api/notifications              # Send notification
```

---

## SDK and Client Libraries

### Typed HTTP Client (C#/Blazor)

The GridPulse UI uses a typed HTTP client:

```csharp
public sealed class GridPulseApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<OutageSummaryResponse>> GetRecentOutagesAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await httpClient
            .GetFromJsonAsync<IReadOnlyList<OutageSummaryResponse>>(
                "api/outages", 
                cancellationToken)
            .ConfigureAwait(false);
        return items ?? Array.Empty<OutageSummaryResponse>();
    }
}
```

**Configuration**:

```json
{
  "Api": {
    "BaseAddress": "https://localhost:7143"
  }
}
```

### JavaScript/TypeScript

Example usage with fetch:

```typescript
async function getRecentOutages(): Promise<OutageSummary[]> {
  const response = await fetch('https://localhost:7143/api/outages');
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }
  return await response.json();
}

async function getOutageById(id: string): Promise<OutageSummary | null> {
  const response = await fetch(`https://localhost:7143/api/outages/${id}`);
  if (response.status === 404) {
    return null;
  }
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }
  return await response.json();
}
```

---

## Testing the API

### Using cURL

```bash
# Health check
curl https://localhost:7143/api/health

# List outages
curl https://localhost:7143/api/outages

# Get specific outage
curl https://localhost:7143/api/outages/17aa5a0f-4f5f-45ec-8dc0-1b53e876c111
```

### Using PowerShell

```powershell
# Health check
Invoke-RestMethod -Uri "https://localhost:7143/api/health"

# List outages
Invoke-RestMethod -Uri "https://localhost:7143/api/outages"

# Get specific outage
$id = "17aa5a0f-4f5f-45ec-8dc0-1b53e876c111"
Invoke-RestMethod -Uri "https://localhost:7143/api/outages/$id"
```

### Using HTTP File (VS Code REST Client)

See `GridPulse.WebApi.http` in the WebApi project:

```http
@GridPulse_WebApi_HostAddress = https://localhost:7143

GET {{GridPulse_WebApi_HostAddress}}/api/health

###

GET {{GridPulse_WebApi_HostAddress}}/api/outages

###

GET {{GridPulse_WebApi_HostAddress}}/api/outages/17aa5a0f-4f5f-45ec-8dc0-1b53e876c111
```

---

## Performance Considerations

### Current Implementation

- In-memory data store (extremely fast)
- No caching layer
- Synchronous response generation

### Future Optimizations

- Response caching for frequently accessed data
- Database connection pooling
- Pagination for large result sets
- ETags for conditional requests
- Compression (gzip, brotli)

---

## Security Best Practices

When consuming the API:

1. **HTTPS Only**: Always use HTTPS in production
2. **Validate Input**: Client-side validation before API calls
3. **Handle Errors**: Gracefully handle error responses
4. **Timeout**: Set appropriate request timeouts
5. **Rate Limiting**: Respect rate limits (when implemented)
6. **Authentication**: Store and transmit tokens securely (when implemented)

---

## Support and Feedback

For API issues or questions:

- Check the [Scalar documentation](https://localhost:7143/scalar/v1) for interactive examples
- Review the [OpenAPI specification](https://localhost:7143/openapi/v1.json)
- See the [Architecture documentation](architecture.md) for system design
