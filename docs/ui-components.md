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

The GridPulse UI is built with **Blazor** using Radzen components exclusively. Components follow **Atomic Design** principles, creating a hierarchical component structure from simple to complex.

### Technology Stack
- **Blazor Interactive Server** - Primary render mode for complex interactions
- **Radzen Blazor Components** - UI component library (REQUIRED - no raw HTML allowed)
- **CSS Design Tokens** - Theme-aware styling with CSS variables
- **Typed HTTP Client** - API integration via `GridPulseApiClient`

### Critical Component Rules

⚠️ **NEVER use raw HTML elements** in Razor components. Always use Radzen equivalents:
- `<h1>` → `<RadzenText TextStyle="TextStyle.H1" TagName="TagName.H1">`
- `<p>` → `<RadzenText TextStyle="TextStyle.Body1">`
- `<label>` → `<RadzenLabel Text="..." Component="field-id">`
- `<div>` → `<RadzenStack>`, `<RadzenRow>`, `<RadzenColumn>`, or `<RadzenCard>`
- `<ul>/<li>` → `<RadzenStack>` with child elements

See [Blazor & Radzen Lessons Learned](blazor-radzen-lessons-learned.md) for detailed migration patterns.

### Render Modes
- **InteractiveServer**: Required for pages with complex state/interactions (Dashboard, Tickets, DispatchBoard)
- **InteractiveAuto**: Use sparingly - can cause hydration issues with Radzen components
- **Static**: For simple content-only pages

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

Visual indicator for outage/ticket status with color coding.

**Location**: `GridPulse.Web/Components/Atoms/StatusChip.razor`

**Purpose**: Display status with appropriate Radzen badge styling

**Parameters**:
```csharp
[Parameter] public string Label { get; set; } = string.Empty;
[Parameter] public string Variant { get; set; } = "default";
```

**Status Variants**:
| Variant | Badge Style | Use Case |
|---------|-------------|----------|
| success | `BadgeStyle.Success` | Resolved, Restored |
| warning | `BadgeStyle.Warning` | Open, Pending |
| info | `BadgeStyle.Info` | InProgress, Acknowledged |
| danger | `BadgeStyle.Danger` | High Priority |
| default | `BadgeStyle.Secondary` | Default state |

**Usage**:
```razor
<StatusChip Label="@ticket.Status.ToString()" Variant="@GetStatusVariant(ticket.Status)" />
```

**Implementation**:
- Uses `<RadzenBadge>` component (never `<span class="chip">`)
- Supports `Variant.Flat` for modern styling
- Text automatically formatted

---

### KPI Cards

Key Performance Indicator display with title, value, and trend.

**Location**: Inline in `Dashboard.razor` (uses `RadzenCard` + `RadzenStack`)

**Purpose**: Display important metrics on dashboard

**Implementation Pattern**:
```razor
<RadzenCard class="kpi-card" Style="padding: var(--gp-space-xl); ...">
    <RadzenStack Gap="var(--gp-space-sm)">
        <span class="kpi-card__title">Open Tickets</span>
        <div class="kpi-card__value-group">
            <span class="kpi-card__value">@OpenTicketsCount</span>
        </div>
        <span class="kpi-card__trend">@HighPriorityCount high priority</span>
    </RadzenStack>
</RadzenCard>
```

**Design Tokens Used**:
- `var(--gp-space-xl)` - Card padding
- `var(--gp-space-sm)` - Internal gaps
- `var(--gp-text-secondary)` - Title color
- `var(--gp-text-primary)` - Value color

**Note**: KPI cards use inline `<span>` for specific styled elements within `RadzenStack`. For semantic text, prefer `<RadzenText>`.

---

## Molecules

Composite components that combine multiple atoms or simple elements.

### OutageSummaryCard

Card displaying outage summary with key details using only Radzen components.

**Location**: `GridPulse.Web/Components/Molecules/OutageSummaryCard.razor`

**Purpose**: Show outage highlights in a card format

**Parameters**:
```csharp
[Parameter, EditorRequired] public OutageSummaryResponse Outage { get; set; } = default!;
```

**Displays** (all using Radzen components):
- Service address: `<RadzenText TextStyle="TextStyle.H6" TagName="TagName.H4">`
- Timestamp: `<RadzenText TextStyle="TextStyle.Caption">`
- Status badge: `<StatusChip>`
- ETA/Affected/Cause: `<RadzenLabel>` + `<RadzenText TextStyle="TextStyle.Subtitle1">`

**Usage**:
```razor
<OutageSummaryCard Outage="@outage" />
```

**Critical Rules**:
- ❌ No `<h4>`, `<small>`, `<strong>`, or `<span>` elements
- ✅ Use `<RadzenText>` with appropriate `TextStyle` and `TagName`
- ✅ Use `<RadzenLabel>` for field labels with `Style="font-size: 0.8rem; text-transform: uppercase;"`
- Layout uses `<RadzenCard>` with nested `<div class="outage-card__header">` and `<div class="outage-card__body">`

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
   - Pages: Route endpoints with `@page` directive

2. **Naming Conventions**:
   - PascalCase for component names
   - Descriptive, action-oriented names
   - Suffix with component type (Card, Grid, Panel, etc.)

3. **Parameter Guidelines**:
   - Use `[Parameter]` attribute
   - Use `[Parameter, EditorRequired]` for required parameters
   - Provide default values: `= Array.Empty<T>()` or `= default!`
   - Document expected values in XML comments

4. **State Management**:
   - Keep state in pages, not templates/organisms
   - Pass data down via parameters
   - Emit events up via `EventCallback<T>`
   - Use `CancellationTokenSource` for async operations that can be cancelled

5. **Radzen Component Requirements** (CRITICAL):
   - ❌ **NEVER** use raw HTML: `<h1>`, `<p>`, `<span>`, `<div>`, `<label>`, `<ul>`, `<li>`, `<button>`
   - ✅ **ALWAYS** use Radzen equivalents:
     - Text: `<RadzenText TextStyle="..." TagName="...">`
     - Layout: `<RadzenStack>`, `<RadzenRow>`, `<RadzenColumn>`, `<RadzenSplitter>`
     - Labels: `<RadzenLabel Text="..." Component="field-id">`
     - Buttons: `<RadzenButton Icon="..." Click="...">`
     - Badges: `<RadzenBadge BadgeStyle="..." Text="...">`
     - Icons: `<RadzenIcon Icon="icon_name">`
     - Forms: `<RadzenTemplateForm>` with `<DataAnnotationsValidator />`
     - Modals: `DialogService.OpenAsync()` (never custom backdrop HTML)
   - See [Blazor & Radzen Lessons Learned](blazor-radzen-lessons-learned.md) for migration examples

### Render Mode Guidelines

**Pages with Complex Interactions**:
```razor
@page "/tickets"
@rendermode InteractiveServer
```

Use `InteractiveServer` for:
- Pages with forms and validation
- Real-time data updates
- Complex state management
- Radzen DataGrid with sorting/paging
- Dialog interactions

**Static Pages** (rare):
```razor
@page "/about"
```

Use static rendering only for:
- Pure content pages
- No user interaction required

⚠️ **Avoid `InteractiveAuto`**: Can cause hydration issues with Radzen components. Use `InteractiveServer` instead.

### Example Component Template

```razor
@* Molecules/ExampleCard.razor *@
<RadzenCard Style="padding: var(--gp-space-md);">
    <RadzenStack Gap="var(--gp-space-sm)">
        <RadzenText TextStyle="TextStyle.H6" TagName="TagName.H4">@Title</RadzenText>
        <RadzenText TextStyle="TextStyle.Body2">@Description</RadzenText>
        @if (ShowBadge)
        {
            <RadzenBadge BadgeStyle="BadgeStyle.Info" Text="@BadgeText" />
        }
    </RadzenStack>
</RadzenCard>

@code {
    [Parameter, EditorRequired]
    public string Title { get; set; } = default!;
    
    [Parameter]
    public string Description { get; set; } = string.Empty;
    
    [Parameter]
    public bool ShowBadge { get; set; }
    
    [Parameter]
    public string BadgeText { get; set; } = string.Empty;
}
```

**Key Points**:
- Uses `<RadzenCard>` instead of `<div>`
- Uses `<RadzenStack>` for layout with design token gaps
- Uses `<RadzenText>` with proper `TextStyle` and `TagName`
- Uses `<RadzenBadge>` instead of custom HTML
- Required parameters use `[Parameter, EditorRequired]` and `= default!`
- Optional parameters have sensible defaults

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

## Pre-Implementation Checklist

Before implementing new UI features:

- [ ] Confirm all text uses `<RadzenText>` with appropriate `TextStyle` and `TagName`
- [ ] Verify no raw HTML layout elements (`<div>`, `<ul>`, `<li>`, `<button>`)
- [ ] Use `<RadzenStack>`/`<RadzenRow>`/`<RadzenColumn>` for all layout
- [ ] Forms use `<RadzenTemplateForm>` with `<DataAnnotationsValidator />` and per-field `<ValidationMessage>`
- [ ] Modals use `DialogService.OpenAsync()` instead of custom backdrop HTML
- [ ] Render mode is `@rendermode InteractiveServer` for complex interactions (document why if using `InteractiveAuto`)
- [ ] All spacing uses design tokens (`var(--gp-space-*)`, `var(--gp-radius-*)`, etc.)
- [ ] Labels use `<RadzenLabel Text="..." Component="field-id">`
- [ ] Badges/chips use `<RadzenBadge>` instead of custom `<span>` elements
- [ ] Icons use `<RadzenIcon Icon="icon_name">` instead of `<i>` or `<span class="bi">`
- [ ] Review [Blazor & Radzen Lessons Learned](blazor-radzen-lessons-learned.md) for migration patterns

## Code Review Checklist

When reviewing UI PRs:

- [ ] No raw HTML text elements (`<h*>`, `<p>`, `<span>`, `<label>`, `<small>`, `<strong>`)
- [ ] No manual layout divs (should use `<RadzenStack>`, `<RadzenRow>`, `<RadzenColumn>`, `<RadzenSplitter>`)
- [ ] No hardcoded spacing (should use design tokens like `var(--gp-space-md)`)
- [ ] Render mode is documented and justified (prefer `InteractiveServer`)
- [ ] Forms have proper validation structure (`<RadzenTemplateForm>`, `<DataAnnotationsValidator />`, per-field `<ValidationMessage>`)
- [ ] Dialogs use `DialogService.OpenAsync()` (no custom modal HTML)
- [ ] All components are Radzen or have documented justification
- [ ] Visual verification included (screenshots/Playwright recordings)
- [ ] Component follows Atomic Design hierarchy (Atom/Molecule/Organism/Template/Page)
- [ ] Parameters use `[Parameter, EditorRequired]` for required props
- [ ] State is managed in pages, not in organisms/molecules
- [ ] References UI Constitution (`UIS/constitution.md`) in PR description

## Testing Components

### Manual Testing

1. Run the application: `aspire run --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj`
2. Navigate to components in browser
3. Test interactions (clicks, form submissions, dialogs)
4. Verify responsive behavior (desktop, tablet, mobile)
5. Test dark/light mode (if theme toggle implemented)
6. Test keyboard navigation and screen reader compatibility

### Automated Testing

**Unit Tests** (bUnit):
- Component rendering tests in `GridPulse.Tests.Unit/Web/Components`
- Mock `GridPulseApiClient` using `TestContext.Services`
- Test parameter binding and event callbacks
- Example: `ComponentTestBase` provides Radzen service mocks

**Integration Tests** (Playwright):
- E2E tests in `tests/GridPulse.Web.Tests.Playwright`
- Test full user workflows (create ticket, dispatch assignment, etc.)
- Use `data-testid` attributes for stable selectors

**Future Testing**:
- Visual regression testing
- Accessibility audits (axe-core)
- Performance profiling (Lighthouse)
