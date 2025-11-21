<!-- markdownlint-disable MD001 MD002 MD003 MD004 MD005 MD006 MD007 MD009 MD010 MD011 MD012 MD013 MD014 MD018 MD019 MD020 MD021 MD022 MD023 MD024 MD025 MD026 MD027 MD028 MD029 MD030 MD031 MD032 MD033 MD034 MD035 MD036 MD037 MD038 MD039 MD040 MD041 MD042 MD043 MD044 MD045 MD046 MD047 MD048 MD049 MD050 MD051 MD052 MD053 MD054 MD055 MD056 MD057 MD058 MD059 MD060 MD061 MD062 MD063 MD064 MD065 MD066 MD067 MD068 -->

# UI Components Documentation

Complete reference for GridPulse Blazor UI components, organized using Atomic Design principles.

## Table of Contents
- [Overview](#overview)
- [Component Architecture](#component-architecture)
- [Atoms](#atoms)
- [Molecules](#molecules)
- [Organisms](#organisms)
- [Templates](#templates)
- [Pages](#pages)
- [Services](#services)
- [Styling](#styling)

---

## Overview

The GridPulse UI is built with **Blazor Interactive Auto**, combining server-side rendering with WebAssembly for optimal performance. Components follow **Atomic Design** principles, creating a hierarchical component structure from simple to complex.

### Technology Stack
- **Blazor Interactive Auto** - Hybrid rendering mode
- **Radzen Blazor Components** - UI component library
- **CSS** - Custom styling
- **Typed HTTP Client** - API integration

### Render Modes
- **Server**: Initial page load (fast first render)
- **WebAssembly**: Client-side after initial load (interactive)
- **Auto**: Blazor chooses optimal mode automatically

---

## Component Architecture

### Atomic Design Hierarchy

```
Pages (Complete Views)
   ↓
Templates (Page Layouts)
   ↓
Organisms (Complex Components)
   ↓
Molecules (Composite Components)
   ↓
Atoms (Basic UI Elements)
```

### Component Organization

```
Components/
├── Atoms/           # Basic building blocks
├── Molecules/       # Simple composites
├── Organisms/       # Complex components
├── Templates/       # Page layouts
├── Pages/           # Complete pages
├── Layout/          # App-level layout
└── _Imports.razor   # Shared imports
```

---

## Atoms

Basic, reusable UI elements that cannot be broken down further.

### StatusChip

Visual indicator for outage status with color coding.

**Location**: `GridPulse.Web/Components/Atoms/StatusChip.razor`

**Purpose**: Display outage status with appropriate styling

**Parameters**:
```csharp
[Parameter] public string Status { get; set; } = string.Empty;
```

**Status Colors**:
| Status | Color | Badge Style |
|--------|-------|-------------|
| Reported | Default/Gray | `Default` |
| Acknowledged | Blue | `Info` |
| CrewDispatched | Yellow | `Warning` |
| Restored | Green | `Success` |

**Usage**:
```razor
<StatusChip Status="@outage.Status" />
```

**Renders**:
- Radzen badge component
- Capitalized status text
- Color-coded background

---

### KpiStat

Key Performance Indicator display with title, value, and trend.

**Location**: `GridPulse.Web/Components/Atoms/KpiStat.razor`

**Purpose**: Display important metrics on dashboard

**Parameters**:
```csharp
[Parameter] public string Title { get; set; } = string.Empty;
[Parameter] public string Value { get; set; } = string.Empty;
[Parameter] public string Trend { get; set; } = string.Empty;
```

**Usage**:
```razor
<KpiStat 
    Title="Active outages" 
    Value="@ActiveOutages" 
    Trend="75% crews engaged" />
```

**Renders**:
- Large, bold value
- Descriptive title
- Secondary trend information
- Card-based layout

---

## Molecules

Composite components that combine multiple atoms or simple elements.

### OutageSummaryCard

Card displaying outage summary with key details.

**Location**: `GridPulse.Web/Components/Molecules/OutageSummaryCard.razor`

**Purpose**: Show outage highlights in a card format

**Parameters**:
```csharp
[Parameter] public OutageSummaryResponse Outage { get; set; } = null!;
```

**Displays**:
- Service address (heading)
- Status badge (using `StatusChip`)
- Reported timestamp
- Last updated timestamp
- Estimated restoration time
- Cause description
- Affected customer count

**Usage**:
```razor
<OutageSummaryCard Outage="@outage" />
```

**Features**:
- Radzen card component
- Formatted timestamps
- Conditional display (ETA, Cause may be null)
- Visual hierarchy

---

### UsageTrendCard

Card displaying usage trend chart over time.

**Location**: `GridPulse.Web/Components/Molecules/UsageTrendCard.razor`

**Purpose**: Visualize energy usage trends

**Parameters**:
```csharp
[Parameter] public string Title { get; set; } = string.Empty;
[Parameter] public string DateRange { get; set; } = string.Empty;
[Parameter] public IReadOnlyList<UsageTrendPoint> Data { get; set; } 
    = Array.Empty<UsageTrendPoint>();
```

**Data Model**:
```csharp
public record UsageTrendPoint(DateTime Date, decimal Value);
```

**Usage**:
```razor
<UsageTrendCard 
    Title="Daily demand" 
    DateRange="Jan 1 – Jan 14" 
    Data="@usageTrend" />
```

**Features**:
- Line chart visualization
- Date range display
- Responsive design
- Radzen chart component

**Note**: Currently uses mock data (sine wave pattern)

---

## Organisms

Complex components composed of molecules and atoms.

### OutageGrid

Sortable data grid displaying all outages.

**Location**: `GridPulse.Web/Components/Organisms/OutageGrid.razor`

**Purpose**: Tabular view of all outages with sorting

**Parameters**:
```csharp
[Parameter] public IReadOnlyList<OutageSummaryResponse> Outages { get; set; } 
    = Array.Empty<OutageSummaryResponse>();
```

**Columns**:
| Column | Type | Sortable | Format |
|--------|------|----------|--------|
| Service Address | Text | Yes | - |
| Status | Badge | Yes | Color-coded |
| Reported | DateTime | Yes | Relative time |
| ETA | DateTime | Yes | Relative time |
| Affected | Number | Yes | Numeric |

**Usage**:
```razor
<OutageGrid Outages="@outages" />
```

**Features**:
- Radzen DataGrid component
- Column sorting
- Status badge integration
- Formatted timestamps
- Responsive layout

---

### OutageActivityTimeline

Chronological timeline of recent outage events.

**Location**: `GridPulse.Web/Components/Organisms/OutageActivityTimeline.razor`

**Purpose**: Display recent outage activity in timeline format

**Parameters**:
```csharp
[Parameter] public IReadOnlyList<OutageActivityItem> Items { get; set; } 
    = Array.Empty<OutageActivityItem>();
```

**Data Model**:
```csharp
public record OutageActivityItem(
    string Location,
    string Description,
    DateTimeOffset Timestamp,
    string Variant  // "Success", "Warning", "Info", "Default"
);
```

**Usage**:
```razor
<OutageActivityTimeline Items="@activity" />
```

**Features**:
- Radzen Timeline component
- Color-coded markers
- Relative timestamps
- Scrollable list (shows 6 most recent)

**Item Variants**:
| Variant | Color | Used For |
|---------|-------|----------|
| Success | Green | Restored outages |
| Warning | Yellow | Crews dispatched |
| Info | Blue | Acknowledged outages |
| Default | Gray | Reported outages |

---

### TicketTimeline

Timeline card summarizing ticket automation and crew events in reverse chronological order.

**Location**: `GridPulse.Web/Components/Organisms/TicketTimeline.razor`

**Purpose**: Provide operators/dispatchers a single place to review audit events, duplicate warnings, automation actions, and crew acknowledgements.

**Parameters**:
```csharp
[Parameter] public IReadOnlyCollection<AssignmentEventDto> Events { get; set; } = Array.Empty<AssignmentEventDto>();
```

**Behavior**:
- Orders events descending by `OccurredAt`
- Applies visual variants (warning/success/info/danger/default) based on `AssignmentEventType`
- Shows `StatusChip` per event with contextual title (e.g., “Crew acknowledged”)
- Lists metadata such as actor and detail key/value pairs when provided
- Displays Radzen info alert when no events exist

**Testing Hooks**: `data-testid="ticket-timeline"` for Playwright/bUnit coverage

---

### CrewStatusPanel

Interactive control surface for simulating crew acknowledgements/status transitions.

**Location**: `GridPulse.Web/Components/Organisms/CrewStatusPanel.razor`

**Purpose**: Monitor crew telemetry, surface SLA alerts, and emit status updates back to the API.

**Parameters**:
```csharp
[Parameter] public CrewStatusDto? Crew { get; set; }
[Parameter] public IReadOnlyCollection<AssignmentEventDto> Timeline { get; set; } = Array.Empty<AssignmentEventDto>();
[Parameter] public bool IsSubmitting { get; set; }
[Parameter] public EventCallback<CrewAssignmentStatus> StatusSubmitted { get; set; }
```

**Features**:
- Header displays crew display name + current assignment status badge
- Alert banner warns when acknowledgement is pending/overdue (>=5 minutes)
- Details list highlights ticket count, skills, and last telemetry timestamp
- Dropdown of all `CrewAssignmentStatus` values plus “Send update” CTA
- Emits `StatusSubmitted` event (disabled while submitting to avoid duplicate posts)
- Includes `data-testid` hooks for select, alert, and send button interactions

**Accessibility**:
- Alerts include `role="alert"`
- Buttons and selects disabled when no crew bound or submission in-flight

---

## Templates

Page-level layout templates.

### DashboardTemplate

Three-section dashboard layout template.

**Location**: `GridPulse.Web/Components/Templates/DashboardTemplate.razor`

**Purpose**: Consistent dashboard layout with KPIs, hero content, and details

**Parameters (RenderFragments)**:
```csharp
[Parameter] public RenderFragment? PrimaryKpis { get; set; }
[Parameter] public RenderFragment? HeroContent { get; set; }
[Parameter] public RenderFragment? DetailContent { get; set; }
```

**Layout Structure**:
```
┌─────────────────────────────────────┐
│       Primary KPIs (3 stats)        │
├─────────────┬───────────────────────┤
│             │                       │
│    Hero     │    Hero Content       │
│   Content   │    (Cards)            │
│             │                       │
├─────────────┴───────────────────────┤
│        Detail Content               │
│     (Grid + Timeline)               │
└─────────────────────────────────────┘
```

**Usage**:
```razor
<DashboardTemplate>
    <PrimaryKpis>
        <KpiStat ... />
        <KpiStat ... />
        <KpiStat ... />
    </PrimaryKpis>
    <HeroContent>
        <UsageTrendCard ... />
        <OutageSummaryCard ... />
    </HeroContent>
    <DetailContent>
        <OutageGrid ... />
        <OutageActivityTimeline ... />
    </DetailContent>
</DashboardTemplate>
```

**Features**:
- Responsive grid layout
- Section-based organization
- Radzen Stack for spacing

---

## Pages

Complete page components.

### Dashboard

Main dashboard page showing outage overview.

**Location**: `GridPulse.Web/Components/Pages/Dashboard.razor`  
**Route**: `/`

**Purpose**: Primary landing page with outage monitoring

**State Management**:
```csharp
private IReadOnlyList<OutageSummaryResponse> outages = Array.Empty<OutageSummaryResponse>();
private IReadOnlyList<OutageSummaryResponse> topOutages = Array.Empty<OutageSummaryResponse>();
private IReadOnlyList<UsageTrendPoint> usageTrend = Array.Empty<UsageTrendPoint>();
private IReadOnlyList<OutageActivityItem> activity = Array.Empty<OutageActivityItem>();
private bool isLoading = true;
```

**Computed KPIs**:
- **Active Outages**: Count of non-resolved outages
- **Impacted Customers**: Sum of affected customers
- **Crews Dispatched**: Count of outages with crews
- **Crew Coverage**: Percentage of outages with crews
- **Upcoming Restorations**: Count of restorations due in 2 hours
- **Dispatch Delta**: Change vs. yesterday (mock)

**Data Loading**:
```csharp
protected override async Task OnInitializedAsync()
{
    outages = await ApiClient.GetRecentOutagesAsync();
    topOutages = outages
        .OrderByDescending(o => o.AffectedCustomers)
        .Take(3)
        .ToArray();
    usageTrend = BuildUsageTrend(outages);
    activity = BuildActivity(outages);
    isLoading = false;
}
```

**Loading State**:
- Shows `RadzenSkeleton` while loading
- Shows `RadzenAlert` if no outages
- Shows dashboard template when data loaded

**Features**:
- Real-time data from API
- Calculated KPIs
- Top 3 outages by impact
- Recent activity timeline
- Mock usage trend (14 days)

---

### Tickets

Operator workspace for triaging outage-derived tickets, running duplicate detection, and promoting workflow states.

**Location**: `GridPulse.Web/Components/Pages/Tickets.razor`  
**Route**: `/tickets`

**Key Features**:
- Filter controls (status multi-select, priority dropdown, timeline/recommendation switches)
- Inline create panel with validation + asset parsing helper
- DataGrid with paging/sorting + `data-testid="tickets-grid"`
- Detail card showing metadata, duplicate warnings, `TicketTimeline`, and promote/reset actions
- Uses `GridPulseApiClient` for `GetTicketsAsync`, `CreateTicketAsync`, `UpdateTicketStatusAsync`

**State Highlights**:
```csharp
private IEnumerable<TicketStatus> StatusFilter { get; set; } = new[] { TicketStatus.Open, TicketStatus.InProgress };
private bool includeTimeline = true;
private bool includeRecommendations;
private IReadOnlyList<TicketDto> tickets = Array.Empty<TicketDto>();
private TicketDto? selectedTicket;
```

**UX Considerations**:
- Displays Radzen skeletons while fetching
- Alerts for duplicate detection and load errors
- `PromoteTicketAsync` enforces workflow guardrails via server-side validation
- `TicketTimeline` reused to keep automation history consistent

---

### Dispatch Board

Dispatcher-focused hub for reviewing recommendations, publishing assignments, and simulating crew acknowledgements.

**Location**: `GridPulse.Web/Components/Pages/DispatchBoard.razor`  
**Route**: `/dispatch`

**Layout**:
- Left column: ticket selector (priority-sorted) with skeleton fallback
- Right column: recommendations grid, action buttons, `TicketTimeline`, `CrewStatusPanel`
- Toast + dialog overlays for assignment receipts and override reasons

**Data Flow**:
```csharp
private IReadOnlyList<TicketDto> dispatchableTickets = Array.Empty<TicketDto>();
private IReadOnlyList<DispatchRecommendationDto> recommendations = Array.Empty<DispatchRecommendationDto>();
private DispatchRecommendationDto? selectedRecommendation;
private bool isPublishingAssignment;
```

**Interactions**:
- Automatically refreshes recommendations via background loop (45 seconds)
- Warns when telemetry is stale or override reason missing
- `PublishAssignmentAsync` posts to `/api/dispatch/assignments` and surfaces receipt text
- `CrewStatusPanel` binds to the currently active crew for the selected ticket

**Accessibility**:
- Ticket list buttons expose `aria-pressed`
- Recommendation grid uses `role="table"` semantics
- Crew status empty state uses descriptive text for assistive tech

---

### Error

Error page for unhandled exceptions.

**Location**: `GridPulse.Web/Components/Pages/Error.razor`  
**Route**: `/error`

**Purpose**: Graceful error handling

---

### NotFound

404 page for missing routes.

**Location**: `GridPulse.Web/Components/Pages/NotFound.razor`  
**Route**: `/404`

**Purpose**: Handle unknown URLs

---

## Services

### GridPulseApiClient

Typed HTTP client for API communication.

**Location**: `GridPulse.Web/Services/GridPulseApiClient.cs`

**Purpose**: Strongly-typed API access from UI

**Methods**:
```csharp
public Task<IReadOnlyList<OutageSummaryResponse>> GetRecentOutagesAsync(
    CancellationToken cancellationToken = default);
public Task<TicketCollectionResponse> GetTicketsAsync(
    TicketFilter filter,
    CancellationToken cancellationToken = default);
public Task<TicketDto> CreateTicketAsync(
    TicketCreateRequest request,
    CancellationToken cancellationToken = default);
public Task<TicketDto> UpdateTicketStatusAsync(
    Guid ticketId,
    TicketStatusUpdateRequest request,
    CancellationToken cancellationToken = default);
public Task<DispatchRecommendationsEnvelope> GetRecommendationsAsync(
    Guid ticketId,
    CancellationToken cancellationToken = default);
public Task<AssignmentReceiptDto> PublishAssignmentAsync(
    DispatchAssignmentRequest request,
    CancellationToken cancellationToken = default);
public Task<CrewStatusUpdateResponse> PostCrewStatusAsync(
    Guid crewId,
    CrewStatusUpdateRequest request,
    CancellationToken cancellationToken = default);
```

**Configuration**:
```json
{
  "Api": {
    "BaseAddress": "https://localhost:7143"
  }
}
```

**Registration** (`Program.cs`):
```csharp
builder.Services.AddHttpClient<GridPulseApiClient>(client =>
{
    var apiBaseAddress = builder.Configuration["Api:BaseAddress"] 
        ?? "https://localhost:7143";
    client.BaseAddress = new Uri(apiBaseAddress);
});
```

**Features**:
- Configured base URL from appsettings
- JSON serialization with shared DTOs from `GridPulse.Application`
- Null-safe helpers (tickets collection defaults to empty)
- Cancellation token support and CTS resets inside pages
- Centralized error handling for dispatch + crew flows

---

### Models

#### OutageSummaryResponse

**Location**: `GridPulse.Web/Services/Models/OutageSummaryResponse.cs`

```csharp
public sealed record OutageSummaryResponse(
    Guid Id,
    Guid ServiceLocationId,
    string ServiceAddress,
    string Status,
    DateTimeOffset ReportedAt,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset? EstimatedRestoration,
    string? Cause,
    int AffectedCustomers
);
```

**Note**: Mirrors API `OutageSummary` but with string-based Status

#### TicketDto (shared application model)

**Namespace**: `GridPulse.Application.Models`

Key properties surfaced in UI:
- `Guid Id`, `string Title`, `TicketStatus Status`, `TicketPriority Priority`
- `int CustomerImpact`, `Guid? AssignedCrewId`, `string? AssignedCrewName`
- `IReadOnlyList<string> AffectedAssets`
- `IReadOnlyCollection<AssignmentEventDto> Events`
- `IReadOnlyCollection<DispatchRecommendationDto> Recommendations`

#### DispatchRecommendationDto

Represents ranked crew options emitted by automation.

Fields consumed by Dispatch Board:
- `CrewStatusDto Crew` (display name, skills, telemetry freshness)
- `double CompositeScore` + `Dictionary<string, double> ScoreComponents`
- `int EtaMinutes`
- Flags: `IsAutoSelected`, `IsOverride`, `OverrideReason`

#### AssignmentEventDto

Timeline entries shared between ticket + dispatch screens.

Important members: `AssignmentEventType EventType`, `DateTimeOffset OccurredAt`, `string Actor`, `Dictionary<string, string> Details`, `Guid? CrewId`.

#### CrewStatusDto

Bound to `CrewStatusPanel` and telemetry badges.

Includes `string DisplayName`, `CrewAssignmentStatus Status`, `int CurrentTicketCount`, `IReadOnlyList<CrewSkill> Skills`, `DateTimeOffset LastStatusUpdate`, `CrewLocationSnapshot? Location`, `bool IsTelemetryStale`.

---

## Layout

### MainLayout

**Location**: `GridPulse.Web/Components/Layout/MainLayout.razor`

**Purpose**: Application-wide layout wrapper

**Features**:
- Navigation menu
- Header
- Main content area
- Footer

---

### NavMenu

**Location**: `GridPulse.Web/Components/Layout/NavMenu.razor`

**Purpose**: Site navigation

**Links**:
- Dashboard (/)
- Other pages (as added)

---

### ReconnectModal

**Location**: `GridPulse.Web/Components/Layout/ReconnectModal.razor`

**Purpose**: Display when SignalR connection lost (Blazor Server)

---

## Styling

### CSS Organization

**Global Styles**: `wwwroot/app.css`

**Component Scoping**: Use `.razor.css` files for scoped styles

**Key Classnames**:
- `.tickets-page`, `.tickets-grid-card`, `.tickets-detail-card`, `.ticket-timeline-card`
- `.dispatch-page`, `.dispatch-recommendations-grid`, `.dispatch-ticket-list`
- `.crew-status-panel`, `.crew-status-panel__alert` for acknowledgment warnings

### Radzen Theme

The application uses Radzen's default theme with customizations.

**Theme File**: Included via Radzen NuGet package

### Responsive Design

- Uses CSS Grid and Flexbox
- Radzen responsive utilities
- Mobile-first approach

---

## Component Guidelines

### Creating New Components

1. **Choose the Right Level**:
   - Atoms: Single-purpose, no dependencies
   - Molecules: Combine 2-3 atoms
   - Organisms: Complex, feature-rich
   - Templates: Layout only
   - Pages: Route endpoints

2. **Naming Conventions**:
   - PascalCase for component names
   - Descriptive, action-oriented names
   - Suffix with component type (Card, Grid, etc.)

3. **Parameter Guidelines**:
   - Use `[Parameter]` attribute
   - Provide default values
   - Use null-forgiving operator (`= null!`) for required parameters
   - Document expected values

4. **State Management**:
   - Keep state in pages, not templates
   - Pass data down via parameters
   - Emit events up via EventCallback

### Example Component Template

```razor
@* Atoms/ExampleAtom.razor *@
<div class="example-atom">
    <span>@Text</span>
</div>

@code {
    [Parameter] 
    public string Text { get; set; } = string.Empty;
}
```

---

## Data Flow

### Top-Down Data Flow

```
Page (Dashboard)
   ↓ (API Call)
API Client
   ↓ (Data)
Page State
   ↓ (Parameters)
Template
   ↓ (Parameters)
Organisms
   ↓ (Parameters)
Molecules
   ↓ (Parameters)
Atoms
```

### Example Flow

1. **Dashboard** calls `ApiClient.GetRecentOutagesAsync()`
2. **API Client** fetches data from WebAPI
3. **Dashboard** stores data in `outages` state
4. **Dashboard** passes data to `DashboardTemplate`
5. **Template** passes data to `OutageGrid`
6. **OutageGrid** renders `StatusChip` atoms

---

## Performance Considerations

### Loading States

Always show loading indicators:
```razor
@if (isLoading)
{
    <RadzenSkeleton Width="100%" Height="320px" />
}
else
{
    <!-- Content -->
}
```

### Empty States

Handle empty data gracefully:
```razor
else if (outages.Count == 0)
{
    <RadzenAlert Severity="AlertSeverity.Info" 
                 Summary="All clear" 
                 Detail="No active outages." />
}
```

### Computed Properties

Use computed properties for derived data:
```csharp
private string ActiveOutages => 
    outages.Count(o => o.Status != "Restored").ToString("N0");
```

### Async Loading

Use `OnInitializedAsync` for data loading:
```csharp
protected override async Task OnInitializedAsync()
{
    outages = await ApiClient.GetRecentOutagesAsync();
    isLoading = false;
}
```

---

## Future Enhancements

Planned component additions:

1. **Customer Portal Components**
   - Account settings
   - Service location management
   - Payment history

2. **Advanced Visualizations**
   - Map-based outage view
   - Real-time updates via SignalR
   - Advanced charting

3. **Operator Tools**
   - Crew dispatch interface
   - Outage creation/editing forms
   - Bulk operations

4. **Notifications**
   - Toast notifications
   - Alert banners
   - Preference editor

---

## Testing Components

### Manual Testing

1. Run the application: `aspire run`
2. Navigate to components
3. Test interactions
4. Verify responsive behavior

### Future Testing

- Unit tests with bUnit
- E2E tests with Playwright
- Visual regression testing
