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
public async Task<IReadOnlyList<OutageSummaryResponse>> GetRecentOutagesAsync(
    CancellationToken cancellationToken = default)
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
- JSON deserialization
- Null-safe (returns empty array on null)
- Cancellation token support

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
