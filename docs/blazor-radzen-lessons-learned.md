# Blazor & Radzen Lessons Learned

## Overview
This document captures critical lessons learned from fixing widespread HTML vs. Radzen component issues across the GridPulse application. These lessons have been incorporated into the constitution and copilot instructions to prevent recurrence.

## Critical Issues Discovered

### 1. Raw HTML Elements in Razor Components

**Problem**: Multiple components used raw HTML elements (`<h1>`, `<h4>`, `<p>`, `<span>`, `<small>`, `<strong>`, `<label>`, `<ul>`, `<li>`) instead of Radzen components.

**Why It's Wrong**:
- Breaks Radzen's theming and styling consistency
- Prevents proper dark/light mode transitions
- Causes accessibility issues
- Violates the component framework architecture

**Fixed Examples**:

```razor
<!-- ❌ WRONG -->
<h4>@Crew?.DisplayName</h4>
<p class="dispatch-subtitle">Monitor acknowledgements...</p>
<label>Crew status</label>

<!-- ✅ CORRECT -->
<RadzenText TextStyle="TextStyle.H6" TagName="TagName.H4">@Crew?.DisplayName</RadzenText>
<RadzenText TextStyle="TextStyle.Body2" Class="dispatch-subtitle">Monitor acknowledgements...</RadzenText>
<RadzenLabel Text="Crew status" Component="crew-status-select" />
```

**Files Fixed**:
- `CrewStatusPanel.razor`
- `TicketTimeline.razor`
- `OutageActivityTimeline.razor`
- `OutageSummaryCard.razor`
- `UsageTrendCard.razor`
- `Dashboard.razor`
- `DispatchBoard.razor`
- `Tickets.razor`

---

### 2. Lists Using HTML Instead of Radzen Components

**Problem**: Used `<ul>` and `<li>` tags instead of `RadzenStack` with proper children.

**Why It's Wrong**:
- Inconsistent spacing
- Breaks responsive behavior
- No theme support

**Fixed Example**:

```razor
<!-- ❌ WRONG -->
<ul>
    <li><strong>Tickets:</strong> @Crew.CurrentTicketCount active</li>
    <li><strong>Skills:</strong> @string.Join(", ", Crew.Skills)</li>
</ul>

<!-- ✅ CORRECT -->
<RadzenStack Gap="8px">
    <RadzenStack Orientation="Orientation.Horizontal" Gap="4px">
        <RadzenText TextStyle="TextStyle.Subtitle2">Tickets:</RadzenText>
        <RadzenText TextStyle="TextStyle.Body2">@Crew.CurrentTicketCount active</RadzenText>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" Gap="4px">
        <RadzenText TextStyle="TextStyle.Subtitle2">Skills:</RadzenText>
        <RadzenText TextStyle="TextStyle.Body2">@string.Join(", ", Crew.Skills)</RadzenText>
    </RadzenStack>
</RadzenStack>
```

---

### 3. Render Mode Issues

**Problem**: Used `@rendermode InteractiveAuto` which caused hydration issues with Radzen components.

**Why It's Wrong**:
- Radzen components rely on proper server/WASM hydration
- InteractiveAuto can cause component initialization timing issues
- State synchronization problems between server and client

**Fixed**:

```razor
<!-- ❌ WRONG -->
@page "/dispatch"
@rendermode InteractiveAuto

<!-- ✅ CORRECT -->
@page "/dispatch"
@rendermode InteractiveServer
```

**Files Fixed**:
- `DispatchBoard.razor`
- `Tickets.razor`

---

### 4. Custom Modal HTML Instead of DialogService

**Problem**: Created custom modal dialog HTML with backdrop divs.

**Why It's Wrong**:
- Duplicates framework functionality
- Accessibility issues (focus trap, ARIA)
- Theming inconsistencies

**Fixed Example**:

```razor
<!-- ❌ WRONG -->
@if (isOverrideDialogOpen)
{
    <div class="dispatch-override-dialog__backdrop">
        <div class="dispatch-override-dialog">
            <div class="dispatch-override-dialog__header">
                <h3>Override recommendation</h3>
                <button @onclick="CloseOverrideDialog">✕</button>
            </div>
            <p>Provide justification...</p>
            <RadzenTextArea @bind-Value="overrideReason" />
        </div>
    </div>
}

<!-- ✅ CORRECT -->
private async Task OpenOverrideDialog()
{
    var result = await DialogService.OpenAsync("Override recommendation", ds =>
        @<RadzenStack Gap="1.5rem">
            <RadzenText TextStyle="TextStyle.Body2">Provide a brief justification...</RadzenText>
            
            <RadzenStack Gap="0.5rem">
                <RadzenLabel Text="Override reason" Component="override-reason" />
                <RadzenTextArea id="override-reason"
                                @bind-Value="@overrideReason"
                                Rows="3"
                                Placeholder="Explain..." />
            </RadzenStack>

            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem">
                <RadzenButton Icon="send" 
                              Click="async () => { await PublishAssignmentAsync(true); ds.Close(true); }">
                    Send
                </RadzenButton>
                <RadzenButton ButtonStyle="ButtonStyle.Light" 
                              Click="() => ds.Close(false)">
                    Cancel
                </RadzenButton>
            </RadzenStack>
        </RadzenStack>,
        new DialogOptions { Width = "500px" }
    );
}
```

**Files Fixed**:
- `DispatchBoard.razor`

---

### 5. CSS Grid/Flexbox Instead of Radzen Layout Components

**Problem**: Used manual CSS layout (`<div class="dispatch-layout">`) instead of Radzen components.

**Why It's Wrong**:
- Loses responsive behavior
- Inconsistent breakpoints
- Harder to maintain

**Fixed Example**:

```razor
<!-- ❌ WRONG -->
<div class="dispatch-layout">
    <div class="dispatch-ticket-card">...</div>
    <div class="dispatch-detail-column">...</div>
</div>

<!-- ✅ CORRECT -->
<RadzenSplitter Orientation="Orientation.Horizontal">
    <RadzenSplitterPane Size="30%" Min="20%" Max="40%">
        <!-- Ticket list -->
    </RadzenSplitterPane>
    <RadzenSplitterPane>
        <!-- Details -->
    </RadzenSplitterPane>
</RadzenSplitter>
```

**Files Fixed**:
- `DispatchBoard.razor`
- `Tickets.razor`

---

### 6. Form Validation Structure

**Problem**: Missing per-field validation messages, relying only on `ValidationSummary`.

**Why It's Wrong**:
- Poor user experience (errors not next to fields)
- Accessibility issues

**Fixed Example**:

```razor
<!-- ❌ WRONG -->
<RadzenTemplateForm TItem="TicketCreateFormModel" Data="@createModel">
    <ValidationSummary />
    <RadzenTextBox @bind-Value="createModel.Title" />
    <RadzenTextBox @bind-Value="createModel.OutageReferenceId" />
</RadzenTemplateForm>

<!-- ✅ CORRECT -->
<RadzenTemplateForm TItem="TicketCreateFormModel" Data="@createModel" Submit="CreateTicketAsync">
    <DataAnnotationsValidator />
    <RadzenStack Gap="12px">
        <RadzenStack Gap="0.25rem">
            <RadzenTextBox @bind-Value="createModel.Title" Name="Title" Placeholder="Ticket title" />
            <ValidationMessage For="@(() => createModel.Title)" />
        </RadzenStack>
        <RadzenStack Gap="0.25rem">
            <RadzenTextBox @bind-Value="createModel.OutageReferenceId" Name="OutageReferenceId" />
            <ValidationMessage For="@(() => createModel.OutageReferenceId)" />
        </RadzenStack>
    </RadzenStack>
</RadzenTemplateForm>
```

**Files Fixed**:
- `Tickets.razor`

---

### 7. Hardcoded Spacing Instead of Design Tokens

**Problem**: Used hardcoded pixel values (`Gap="8px"`, `Gap="16px"`) instead of design tokens.

**Why It's Wrong**:
- Inconsistent spacing across app
- Can't adapt to theme changes
- Harder to maintain

**Fixed Example**:

```razor
<!-- ❌ WRONG -->
<RadzenStack Gap="8px">
    <RadzenStack Gap="12px">
        ...
    </RadzenStack>
</RadzenStack>

<!-- ✅ CORRECT -->
<RadzenStack Gap="var(--gp-space-sm)">
    <RadzenStack Gap="var(--gp-space-md)">
        ...
    </RadzenStack>
</RadzenStack>
```

**Files Fixed**:
- `Dashboard.razor`
- Multiple other components

---

## Component Migration Patterns

### Text Elements

| HTML Element | Radzen Component |
|--------------|------------------|
| `<h1>` | `<RadzenText TextStyle="TextStyle.H1" TagName="TagName.H1">` |
| `<h2>` | `<RadzenText TextStyle="TextStyle.H2" TagName="TagName.H2">` |
| `<h3>` | `<RadzenText TextStyle="TextStyle.H5" TagName="TagName.H3">` |
| `<h4>` | `<RadzenText TextStyle="TextStyle.H6" TagName="TagName.H4">` |
| `<p>` | `<RadzenText TextStyle="TextStyle.Body1">` |
| `<span>` | `<RadzenText>` (with appropriate TextStyle) |
| `<small>` | `<RadzenText TextStyle="TextStyle.Caption">` |
| `<strong>` | `<RadzenText TextStyle="TextStyle.Subtitle1">` |
| `<label>` | `<RadzenLabel Text="..." Component="field-id">` |

### Layout Elements

| HTML/CSS Pattern | Radzen Component |
|-----------------|------------------|
| `<div>` (vertical stack) | `<RadzenStack>` |
| `<div>` (horizontal) | `<RadzenStack Orientation="Orientation.Horizontal">` |
| CSS Grid | `<RadzenRow>` + `<RadzenColumn>` |
| Flexbox | `<RadzenStack>` with Orientation/Gap |
| Split panels | `<RadzenSplitter>` + `<RadzenSplitterPane>` |
| Card container | `<RadzenCard>` |

### Data Display

| HTML/CSS Pattern | Radzen Component |
|-----------------|------------------|
| `<ul><li>` | `<RadzenStack>` with children |
| Table | `<RadzenDataGrid>` |
| Badge/chip | `<RadzenBadge>` |
| Icon | `<RadzenIcon Icon="name">` |

---

## Pre-Implementation Checklist

Before implementing new UI features:

- [ ] Confirm all text uses `RadzenText` with appropriate `TextStyle` and `TagName`
- [ ] Verify no raw HTML layout elements (`<div>`, `<ul>`, `<li>`)
- [ ] Use `RadzenStack`/`RadzenRow`/`RadzenColumn` for all layout
- [ ] Forms use `<RadzenTemplateForm>` with per-field `<ValidationMessage>`
- [ ] Modals use `DialogService.OpenAsync()` instead of custom HTML
- [ ] Render mode is `@rendermode InteractiveServer` for complex interactions
- [ ] All spacing uses design tokens (`var(--gp-space-*)`)
- [ ] Labels use `<RadzenLabel>` with `Component` attribute
- [ ] Badges/chips use `<RadzenBadge>` instead of custom spans
- [ ] Icons use `<RadzenIcon>` instead of `<i>` or `<span class="bi">`

---

## Code Review Checklist

When reviewing UI PRs:

- [ ] No raw HTML text elements (`<h*>`, `<p>`, `<span>`, `<label>`)
- [ ] No manual layout divs (should use Radzen layout components)
- [ ] No hardcoded spacing (should use design tokens)
- [ ] Render mode is documented and justified
- [ ] Forms have proper validation structure
- [ ] Dialogs use `DialogService`
- [ ] All components are Radzen or have justification
- [ ] Visual verification included (screenshots/Playwright)

---

## Impact Summary

**Files Modified**: 15+ component files  
**Lines Changed**: 1000+ lines  
**Key Improvements**:
- Consistent theming across all components
- Proper dark/light mode support
- Better accessibility (ARIA, keyboard nav)
- Improved responsiveness
- Easier maintenance
- Better code reuse

---

## References

- [Radzen Blazor Components Documentation](https://blazor.radzen.com/)
- [GridPulse UI Constitution](../UIS/constitution.md)
- [Copilot Instructions](.github/copilot-instructions.md)
- [UI Migration Guide](ui-migration-guide.md)
