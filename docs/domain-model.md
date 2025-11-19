# Domain Model Documentation

This document provides comprehensive documentation of the GridPulse domain model, including all entities, enums, and their relationships.

## Table of Contents
- [Overview](#overview)
- [Domain Entities](#domain-entities)
- [Domain Enums](#domain-enums)
- [Entity Relationships](#entity-relationships)
- [Design Patterns](#design-patterns)

---

## Overview

The GridPulse domain model follows **Domain-Driven Design (DDD)** principles with a focus on immutability and clear business semantics. All domain types are defined in the `GridPulse.Domain` namespace and are framework-agnostic.

### Key Characteristics
- **Immutable by default** - All properties use `init` setters
- **Rich types** - Entities contain business logic where appropriate
- **No infrastructure dependencies** - Pure domain logic
- **Value semantics** - Collections are `IReadOnlyCollection`
- **Explicit relationships** - Using GUIDs for associations

---

## Domain Entities

### Outage

Represents a power outage event affecting one or more customers.

**Namespace**: `GridPulse.Domain.Entities`  
**Type**: `sealed class`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique outage identifier |
| `ServiceLocationId` | `Guid` | Associated service location |
| `ServiceAddress` | `string` | Human-readable service address |
| `Status` | `OutageStatus` | Current outage lifecycle status |
| `ReportedAt` | `DateTimeOffset` | When the outage was first reported |
| `LastUpdatedAt` | `DateTimeOffset` | Most recent status update |
| `EstimatedRestoration` | `DateTimeOffset?` | ETA for power restoration (nullable) |
| `Cause` | `string?` | Root cause description (nullable) |
| `Events` | `IReadOnlyCollection<OutageEvent>` | Audit trail of status changes and notes |

#### Business Rules

1. **Status Progression**: Outages typically flow: Reported → Acknowledged → CrewDispatched → Restored
2. **Timestamps**: `LastUpdatedAt` must be >= `ReportedAt`
3. **Events**: Event collection maintains chronological history
4. **Address**: `ServiceAddress` provides human-readable location for display

#### Usage Example

```csharp
var outage = new Outage
{
    Id = Guid.NewGuid(),
    ServiceLocationId = locationId,
    ServiceAddress = "123 Main St, Apex, NC",
    Status = OutageStatus.Reported,
    ReportedAt = DateTimeOffset.UtcNow,
    LastUpdatedAt = DateTimeOffset.UtcNow,
    EstimatedRestoration = DateTimeOffset.UtcNow.AddHours(4),
    Cause = "Equipment failure",
    Events = new List<OutageEvent>()
};
```

---

### OutageEvent

Represents a single event in an outage's lifecycle, such as status changes or operator notes.

**Namespace**: `GridPulse.Domain.Entities`  
**Type**: `sealed class`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique event identifier |
| `OutageId` | `Guid` | Parent outage reference |
| `Timestamp` | `DateTimeOffset` | When the event occurred |
| `Type` | `OutageEventType` | Event classification |
| `StatusFrom` | `OutageStatus?` | Previous status (for StatusChange events) |
| `StatusTo` | `OutageStatus?` | New status (for StatusChange events) |
| `Message` | `string?` | Event description or note |
| `CreatedBy` | `string` | User or system that created the event (default: "system") |

#### Event Types

1. **StatusChange**: Records status transitions (StatusFrom/StatusTo populated)
2. **OperatorNote**: Manual notes added by operators (Message populated)

#### Usage Example

```csharp
var statusChangeEvent = new OutageEvent
{
    Id = Guid.NewGuid(),
    OutageId = outageId,
    Timestamp = DateTimeOffset.UtcNow,
    Type = OutageEventType.StatusChange,
    StatusFrom = OutageStatus.Reported,
    StatusTo = OutageStatus.Acknowledged,
    Message = "Outage confirmed by operator",
    CreatedBy = "operator@zavpower.com"
};

var operatorNote = new OutageEvent
{
    Id = Guid.NewGuid(),
    OutageId = outageId,
    Timestamp = DateTimeOffset.UtcNow,
    Type = OutageEventType.OperatorNote,
    Message = "Crew reports tree on power line",
    CreatedBy = "field.crew@zavpower.com"
};
```

---

### Customer

Represents a utility customer who receives power service at one or more locations.

**Namespace**: `GridPulse.Domain.Entities`  
**Type**: `sealed class`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique customer identifier |
| `Email` | `string` | Customer email address |
| `FullName` | `string` | Customer's full name |
| `PhoneNumber` | `string` | Contact phone number |
| `PreferredNotificationChannel` | `NotificationChannel` | Default notification method |
| `CreatedAt` | `DateTimeOffset` | Account creation timestamp |
| `UpdatedAt` | `DateTimeOffset` | Last profile update |
| `ServiceLocations` | `IReadOnlyCollection<ServiceLocation>` | Associated service addresses |

#### Business Rules

1. **Email**: Should be unique across customers (enforced at application layer)
2. **Notification**: `PreferredNotificationChannel` defaults to Email
3. **Service Locations**: A customer can have multiple service locations
4. **Contact**: At least one contact method (email or phone) should be provided

#### Usage Example

```csharp
var customer = new Customer
{
    Id = Guid.NewGuid(),
    Email = "john.doe@example.com",
    FullName = "John Doe",
    PhoneNumber = "+1-555-0123",
    PreferredNotificationChannel = NotificationChannel.Email,
    CreatedAt = DateTimeOffset.UtcNow,
    UpdatedAt = DateTimeOffset.UtcNow,
    ServiceLocations = new List<ServiceLocation>()
};
```

---

### ServiceLocation

Represents a physical address where electrical service is provided.

**Namespace**: `GridPulse.Domain.Entities`  
**Type**: `sealed class`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique location identifier |
| `CustomerId` | `Guid` | Owning customer reference |
| `AddressLine1` | `string` | Primary address line |
| `AddressLine2` | `string?` | Secondary address line (apt, suite, etc.) |
| `City` | `string` | City name |
| `State` | `string` | State/province code |
| `PostalCode` | `string` | ZIP/postal code |
| `MeterId` | `string` | Physical meter identifier |
| `IsPrimary` | `bool` | Whether this is the customer's primary location |
| `UsageReadings` | `IReadOnlyCollection<UsageReading>` | Historical usage data |

#### Business Rules

1. **Primary Location**: Each customer should have exactly one primary location
2. **Meter ID**: Must be unique across all service locations
3. **Address**: Complete address required for service delivery
4. **Usage**: Usage readings are associated with this location

#### Usage Example

```csharp
var serviceLocation = new ServiceLocation
{
    Id = Guid.NewGuid(),
    CustomerId = customerId,
    AddressLine1 = "123 Main Street",
    AddressLine2 = "Apt 4B",
    City = "Apex",
    State = "NC",
    PostalCode = "27502",
    MeterId = "METER-12345",
    IsPrimary = true,
    UsageReadings = new List<UsageReading>()
};
```

---

### UsageReading

Represents a single electricity usage measurement at a service location.

**Namespace**: `GridPulse.Domain.Entities`  
**Type**: `sealed class`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique reading identifier |
| `ServiceLocationId` | `Guid` | Associated service location |
| `ReadingDate` | `DateOnly` | Date of the reading |
| `KilowattHours` | `decimal` | Energy consumed in kWh |
| `EstimatedCost` | `decimal?` | Calculated cost (nullable) |

#### Business Rules

1. **Date**: One reading per day per service location
2. **kWh**: Must be non-negative
3. **Cost**: Calculated based on rate schedule (optional)
4. **Ordering**: Readings are typically queried in chronological order

#### Usage Example

```csharp
var usageReading = new UsageReading
{
    Id = Guid.NewGuid(),
    ServiceLocationId = locationId,
    ReadingDate = DateOnly.FromDateTime(DateTime.Today),
    KilowattHours = 35.7m,
    EstimatedCost = 4.28m
};
```

---

### NotificationPreference

Represents a customer's preferences for receiving outage and service notifications.

**Namespace**: `GridPulse.Domain.Entities`  
**Type**: `sealed class`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CustomerId` | `Guid` | Associated customer |
| `Channel` | `NotificationChannel` | Preferred delivery channel |
| `PhoneNumber` | `string?` | SMS phone number (required if SMS enabled) |
| `EmailEnabled` | `bool` | Whether to send email notifications |
| `SmsEnabled` | `bool` | Whether to send SMS notifications |

#### Business Rules

1. **Channel**: Defaults to Email
2. **Email**: `EmailEnabled` true by default
3. **SMS**: If `SmsEnabled` is true, `PhoneNumber` must be provided
4. **Multi-channel**: Both email and SMS can be enabled simultaneously

#### Usage Example

```csharp
var preferences = new NotificationPreference
{
    CustomerId = customerId,
    Channel = NotificationChannel.Email,
    PhoneNumber = "+1-555-0123",
    EmailEnabled = true,
    SmsEnabled = true
};
```

---

## Domain Enums

### OutageStatus

Defines the lifecycle states of a power outage.

**Namespace**: `GridPulse.Domain.Enums`  
**Type**: `enum`

#### Values

| Value | Numeric | Description |
|-------|---------|-------------|
| `Reported` | 0 | Initial state when outage is detected or reported |
| `Acknowledged` | 1 | Outage confirmed and logged by operator |
| `CrewDispatched` | 2 | Repair crew assigned and en route to location |
| `Restored` | 3 | Power service fully restored to all affected customers |

#### State Transitions

```
Reported
   ↓
Acknowledged
   ↓
CrewDispatched
   ↓
Restored
```

#### Usage Example

```csharp
var outage = new Outage
{
    Status = OutageStatus.Reported
};

// Later, after acknowledgment
var acknowledgedOutage = outage with 
{ 
    Status = OutageStatus.Acknowledged,
    LastUpdatedAt = DateTimeOffset.UtcNow 
};
```

---

### OutageEventType

Classifies different types of events in an outage's history.

**Namespace**: `GridPulse.Domain.Enums`  
**Type**: `enum`

#### Values

| Value | Numeric | Description |
|-------|---------|-------------|
| `StatusChange` | 0 | Represents a state transition (uses StatusFrom/StatusTo) |
| `OperatorNote` | 1 | Manual note or observation added by an operator |

#### Usage Example

```csharp
// Status change event
var event1 = new OutageEvent
{
    Type = OutageEventType.StatusChange,
    StatusFrom = OutageStatus.Reported,
    StatusTo = OutageStatus.Acknowledged
};

// Operator note
var event2 = new OutageEvent
{
    Type = OutageEventType.OperatorNote,
    Message = "Customer reported downed wire"
};
```

---

### NotificationChannel

Defines available methods for sending notifications to customers.

**Namespace**: `GridPulse.Domain.Enums`  
**Type**: `enum`

#### Values

| Value | Numeric | Description |
|-------|---------|-------------|
| `None` | 0 | No notifications |
| `Email` | 1 | Email delivery |
| `Sms` | 2 | SMS text message delivery |

#### Usage Example

```csharp
var customer = new Customer
{
    PreferredNotificationChannel = NotificationChannel.Email
};

var preference = new NotificationPreference
{
    Channel = NotificationChannel.Sms,
    PhoneNumber = "+1-555-0123",
    SmsEnabled = true
};
```

---

## Entity Relationships

### Entity Relationship Diagram

```
┌─────────────┐
│  Customer   │
│  (1)        │
└──────┬──────┘
       │ 1:N
       │
       ▼
┌─────────────────────┐
│  ServiceLocation    │
│  (N)                │
└──────┬──────────────┘
       │ 1:N
       │
       ├─────────────────┐
       │                 │
       ▼                 ▼
┌──────────────┐  ┌──────────┐
│ UsageReading │  │  Outage  │
│  (N)         │  │  (N)     │
└──────────────┘  └────┬─────┘
                       │ 1:N
                       │
                       ▼
                 ┌─────────────┐
                 │ OutageEvent │
                 │  (N)        │
                 └─────────────┘

┌─────────────┐
│  Customer   │
│  (1)        │
└──────┬──────┘
       │ 1:1
       │
       ▼
┌──────────────────────┐
│ NotificationPref.    │
│  (1)                 │
└──────────────────────┘
```

### Relationship Descriptions

#### Customer → ServiceLocation (1:N)
- One customer can have multiple service locations
- Each service location belongs to exactly one customer
- Foreign key: `ServiceLocation.CustomerId`

#### ServiceLocation → UsageReading (1:N)
- One service location has many usage readings
- Each usage reading belongs to one service location
- Foreign key: `UsageReading.ServiceLocationId`

#### ServiceLocation → Outage (1:N)
- One service location can experience multiple outages
- Each outage affects one specific service location
- Foreign key: `Outage.ServiceLocationId`

#### Outage → OutageEvent (1:N)
- One outage has many events (audit trail)
- Each event belongs to exactly one outage
- Foreign key: `OutageEvent.OutageId`

#### Customer → NotificationPreference (1:1)
- One customer has one notification preference
- Each preference belongs to one customer
- Foreign key: `NotificationPreference.CustomerId`

---

## Design Patterns

### Immutability

All domain entities use `init` setters to enforce immutability:

```csharp
// Immutable by default
public sealed class Outage
{
    public Guid Id { get; init; }
    public OutageStatus Status { get; init; }
    // ... other properties
}

// Creating new instances with modifications
var updated = original with { Status = OutageStatus.Acknowledged };
```

**Benefits**:
- Thread-safe by default
- Simplified reasoning about state changes
- Clear audit trail (new instances for changes)
- Reduced bugs from unexpected mutations

### Value Objects (Collections)

All collections use `IReadOnlyCollection<T>`:

```csharp
public IReadOnlyCollection<OutageEvent> Events { get; init; } 
    = Array.Empty<OutageEvent>();
```

**Benefits**:
- External code cannot modify collections
- Clear ownership semantics
- Defensive programming

### Explicit IDs

All entities use `Guid` for identifiers:

```csharp
public Guid Id { get; init; }
public Guid CustomerId { get; init; }
public Guid ServiceLocationId { get; init; }
```

**Benefits**:
- Globally unique identifiers
- No database dependency for ID generation
- Distributed system friendly
- Easy to test

### Rich Domain Model

Entities contain business logic where appropriate:

```csharp
// Example of potential domain logic (not yet implemented)
public bool CanBeRestored() => 
    Status == OutageStatus.CrewDispatched;

public bool IsOverdue() => 
    EstimatedRestoration.HasValue 
    && EstimatedRestoration.Value < DateTimeOffset.UtcNow;
```

### Sealed Classes

All entities are `sealed` to prevent inheritance:

```csharp
public sealed class Outage { }
```

**Benefits**:
- Clear design intent (composition over inheritance)
- Performance optimizations
- Simplified reasoning

---

## Future Enhancements

Potential additions to the domain model:

1. **Crew Entity** - Track repair crews and assignments
2. **Equipment Entity** - Infrastructure components (transformers, lines)
3. **GeoLocation** - Latitude/longitude for mapping
4. **OutageZone** - Geographic grouping of outages
5. **ServicePlan** - Customer rate plans and tiers
6. **PaymentMethod** - Billing and payment information
7. **WorkOrder** - Maintenance and repair tasks
8. **AlertRule** - Automated notification triggers

---

## Validation

Currently, domain entities do not include built-in validation. Validation is expected to occur at:

1. **Application Layer** - Service methods validate inputs
2. **API Layer** - Request validation before processing
3. **Database Layer** - Constraints and foreign keys

Future consideration: Add domain validation using FluentValidation or similar.
