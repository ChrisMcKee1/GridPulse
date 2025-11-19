# Architecture Overview

## Table of Contents
- [System Architecture](#system-architecture)
- [Clean Architecture Layers](#clean-architecture-layers)
- [Project Structure](#project-structure)
- [Technology Stack](#technology-stack)
- [Design Principles](#design-principles)
- [Integration Points](#integration-points)

## System Architecture

GridPulse follows a **Clean Architecture** pattern with clear separation of concerns across multiple layers. The system is orchestrated using **.NET Aspire 13**, providing cloud-native capabilities and simplified distributed application management.

### High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Aspire AppHost                           │
│                  (GridPulse.AppHost)                        │
│                                                             │
│  ┌──────────────────┐         ┌──────────────────┐        │
│  │   Web Frontend   │────────▶│    Web API       │        │
│  │ (Blazor Auto +   │  HTTPS  │ (Minimal APIs)   │        │
│  │  WASM Client)    │         │                  │        │
│  └──────────────────┘         └──────────────────┘        │
│         │                              │                   │
│         │                              │                   │
└─────────┼──────────────────────────────┼───────────────────┘
          │                              │
          │                              ▼
          │                     ┌─────────────────┐
          │                     │   Application   │
          │                     │     Services    │
          │                     └─────────────────┘
          │                              │
          │                              ▼
          │                     ┌─────────────────┐
          │                     │  Infrastructure │
          └────────────────────▶│  (In-Memory)    │
                                └─────────────────┘
                                         │
                                         ▼
                                ┌─────────────────┐
                                │  Domain Model   │
                                │ (Entities/Enums)│
                                └─────────────────┘
```

## Clean Architecture Layers

### 1. Domain Layer (`GridPulse.Domain`)
**Purpose**: Contains core business entities and domain logic. No external dependencies.

**Responsibilities**:
- Define domain entities (Outage, Customer, ServiceLocation, etc.)
- Define domain enums (OutageStatus, OutageEventType, NotificationChannel)
- Represent business rules through immutable types

**Key Characteristics**:
- Immutable entities using `init` setters
- No infrastructure dependencies
- Pure business logic only
- Framework-agnostic

### 2. Application Layer (`GridPulse.Application`)
**Purpose**: Orchestrates domain logic and defines application use cases.

**Responsibilities**:
- Service interfaces and implementations
- Repository abstractions
- DTOs and view models
- Application-specific business logic

**Key Components**:
- `IOutageReadService` - Query service for outages
- `IOutageRepository` - Repository abstraction
- `OutageSummary` - DTO for outage data transfer
- `ServiceCollectionExtensions` - DI registration

### 3. Infrastructure Layer (`GridPulse.Infrastructure`)
**Purpose**: Implements data access and external service integrations.

**Responsibilities**:
- Repository implementations
- Configuration options
- External service adapters
- Data persistence logic

**Current Implementation**:
- `InMemoryOutageRepository` - Seed data repository (temporary)
- `OracleOptions` - Placeholder for future Oracle integration

**Future Plans**:
- Oracle database implementation using `System.Data.Common`
- Azure Managed Identity for authentication
- Caching layers

### 4. Web API Layer (`GridPulse.WebApi`)
**Purpose**: HTTP REST API using .NET Minimal APIs.

**Responsibilities**:
- HTTP endpoint definitions
- Request/response handling
- OpenAPI documentation
- CORS configuration

**Features**:
- Minimal API architecture
- Scalar for interactive API documentation (replaces Swagger)
- Problem Details for error handling
- JSON enum serialization

**Endpoints**:
- `GET /api/health` - Health check
- `GET /api/outages` - List recent outages
- `GET /api/outages/{id}` - Get outage by ID

### 5. Web UI Layer (`GridPulse.Web`)
**Purpose**: Blazor-based web application with Interactive Auto render mode.

**Architecture**:
- **GridPulse.Web** - Server-side Blazor host
- **GridPulse.Web.Client** - WASM client components

**Responsibilities**:
- User interface components
- API client integration
- Interactive dashboards
- Real-time updates

**UI Component Hierarchy**:
- **Templates**: `DashboardTemplate` - Page layouts
- **Organisms**: `OutageGrid`, `OutageActivityTimeline` - Complex components
- **Molecules**: `OutageSummaryCard`, `UsageTrendCard` - Composite components
- **Atoms**: `StatusChip`, `KpiStat` - Basic UI elements

### 6. AppHost Layer (`GridPulse.AppHost`)
**Purpose**: Aspire orchestration and service configuration.

**Responsibilities**:
- Service discovery and registration
- Inter-service communication
- External endpoint configuration
- Development environment orchestration

**Configuration**:
```csharp
var api = builder.AddProject<Projects.GridPulse_WebApi>("gridpulse-api");
var web = builder.AddProject<Projects.GridPulse_Web>("gridpulse-web")
    .WithReference(api)
    .WithExternalHttpEndpoints();
```

## Project Structure

```
GridPulse/
├── src/
│   ├── GridPulse.Domain/              # Domain entities & enums
│   │   ├── Entities/
│   │   │   ├── Outage.cs
│   │   │   ├── OutageEvent.cs
│   │   │   ├── Customer.cs
│   │   │   ├── ServiceLocation.cs
│   │   │   ├── UsageReading.cs
│   │   │   └── NotificationPreference.cs
│   │   └── Enums/
│   │       ├── OutageStatus.cs
│   │       ├── OutageEventType.cs
│   │       └── NotificationChannel.cs
│   │
│   ├── GridPulse.Application/         # Application services
│   │   ├── Services/
│   │   │   ├── IOutageReadService.cs
│   │   │   └── OutageReadService.cs
│   │   ├── Abstractions/
│   │   │   └── IOutageRepository.cs
│   │   └── Models/
│   │       └── OutageSummary.cs
│   │
│   ├── GridPulse.Infrastructure/      # Data access
│   │   ├── Repositories/
│   │   │   └── InMemoryOutageRepository.cs
│   │   └── Options/
│   │       └── OracleOptions.cs
│   │
│   ├── GridPulse.WebApi/              # REST API
│   │   └── Program.cs
│   │
│   ├── GridPulse.Web/                 # Blazor UI
│   │   ├── GridPulse.Web/             # Server host
│   │   │   ├── Components/
│   │   │   │   ├── Pages/
│   │   │   │   ├── Layout/
│   │   │   │   ├── Templates/
│   │   │   │   ├── Organisms/
│   │   │   │   ├── Molecules/
│   │   │   │   └── Atoms/
│   │   │   └── Services/
│   │   │       └── GridPulseApiClient.cs
│   │   └── GridPulse.Web.Client/      # WASM client
│   │
│   ├── GridPulse.AppHost/             # Aspire orchestration
│   │   └── AppHost.cs
│   │
│   └── GridPulse.Tests.Unit/          # Unit tests
│
├── docs/                              # Documentation
└── README.md                          # Project overview
```

## Technology Stack

### Backend
- **.NET 10** - Latest .NET platform
- **C# 13** - Language features
- **ASP.NET Core Minimal APIs** - HTTP endpoints
- **System.Data.Common** - Database abstraction (future Oracle integration)

### Frontend
- **Blazor Interactive Auto** - Hybrid rendering mode
- **Blazor WebAssembly** - Client-side execution
- **Radzen Blazor Components** - UI component library
- **CSS/HTML5** - Styling and markup

### Infrastructure
- **.NET Aspire 13** - Cloud-native orchestration
- **Scalar** - API documentation (replaces Swagger)
- **In-Memory Storage** - Current data layer (temporary)
- **Oracle Database** - Planned production database

### Development Tools
- **Aspire CLI** - Project management and orchestration
- **xUnit** - Unit testing framework
- **Git** - Version control

## Design Principles

### 1. **Clean Architecture**
- Clear separation of concerns
- Dependency inversion (dependencies point inward)
- Business logic isolated from infrastructure

### 2. **Immutability**
- Domain entities use `init` setters
- Promotes thread safety and predictable behavior
- Simplifies reasoning about state

### 3. **Dependency Injection**
- Constructor injection for all dependencies
- No static singletons
- Microsoft.Extensions.DependencyInjection

### 4. **API-First Design**
- Minimal APIs with explicit contracts
- OpenAPI documentation
- Versioned endpoints (future)

### 5. **Separation of UI and Logic**
- Typed API client (`GridPulseApiClient`)
- No direct database access from UI
- Clear data flow

### 6. **Cloud-Native Patterns**
- Aspire for orchestration
- Health checks
- Configuration management
- Service discovery

## Integration Points

### Current Integrations
1. **Aspire AppHost** ↔ **WebApi** - Service orchestration
2. **Aspire AppHost** ↔ **Web UI** - Service orchestration with API reference
3. **Web UI** ↔ **WebApi** - HTTPS/JSON over configured base URL
4. **WebApi** ↔ **Application Services** - Dependency injection
5. **Application Services** ↔ **Infrastructure** - Repository pattern

### Planned Integrations
1. **Oracle Database** - Production data persistence
2. **Microsoft Entra B2C** - Customer authentication
3. **Microsoft Entra ID** - Operator authentication
4. **Azure Managed Identity** - Secure database connections
5. **Azure Container Apps** - Cloud deployment
6. **SignalR** - Real-time updates
7. **Azure Service Bus** - Event-driven notifications

## Configuration

### API Configuration (`appsettings.json`)
- Logging levels
- CORS policies
- Database connection strings (future)

### Web UI Configuration
- `Api:BaseAddress` - API endpoint (defaults to `https://localhost:7143`)
- Blazor render modes
- Component settings

### Aspire Configuration
- Service endpoints
- Port bindings (API: 7143)
- External HTTP endpoints
- Resource management

## Security Considerations

### Current State
- HTTPS enforcement
- CORS configuration
- Problem details for error handling

### Planned Security Features
- Microsoft Entra authentication
- Role-based authorization (Customer vs Operator)
- API key management
- Rate limiting
- Data encryption at rest
- Secure connection strings via Azure Key Vault

## Scalability and Performance

### Current Approach
- In-memory repository for development
- Async/await throughout
- Minimal API overhead

### Future Optimizations
- Database connection pooling
- Response caching
- CDN for static assets
- Horizontal scaling via Azure Container Apps
- Distributed caching (Redis)

## Monitoring and Observability

### Current Capabilities
- Health check endpoint
- Aspire dashboard
- Application logging

### Planned Enhancements
- Application Insights integration
- Distributed tracing
- Performance metrics
- Alert configurations
