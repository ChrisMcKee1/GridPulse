# Development Guide

Comprehensive guide for setting up and working with the GridPulse codebase.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Development Workflows](#development-workflows)
- [Building and Testing](#building-and-testing)
- [Running the Application](#running-the-application)
- [Debugging](#debugging)
- [Code Standards](#code-standards)
- [Git Workflow](#git-workflow)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software

#### .NET SDK 10.0
- **Version**: 10.0.100-preview or later
- **Download**: https://dotnet.microsoft.com/download/dotnet/10.0
- **Verify**: `dotnet --version`

#### Aspire CLI
- **Version**: 13.0.0 or later
- **Verify**: `aspire --version`
- **Update**: `aspire update --self --quality stable`

#### IDE (Choose One)

**Visual Studio 2022**
- Version 17.13 or later
- Workload: ASP.NET and web development
- Workload: .NET Aspire SDK

**Visual Studio Code**
- C# Dev Kit extension
- Aspire extension (optional but recommended)

**JetBrains Rider**
- Version 2024.3 or later
- .NET Aspire support built-in

### Optional Tools

- **Git** - Version control (required for contributing)
- **Docker** - For future container-based development
- **Azure CLI** - For cloud deployments
- **PowerShell 7+** - For automation scripts

---

## Getting Started

### Clone the Repository

```bash
git clone https://github.com/ChrisMcKee1/GridPulse.git
cd GridPulse
```

### Restore Dependencies

```bash
dotnet restore GridPulse.sln
```

### Build the Solution

```bash
dotnet build GridPulse.sln
```

Expected output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Run Tests

```bash
dotnet test GridPulse.sln
```

### Run the Application

```bash
aspire run --project src/GridPulse.AppHost/GridPulse.AppHost.csproj
```

The Aspire dashboard will open in your browser, showing:
- API endpoint: `https://localhost:7143`
- Web UI endpoint: Auto-assigned by Aspire

---

## Project Structure

### Solution Organization

```
GridPulse/
├── src/
│   ├── GridPulse.Domain/              # Core domain models
│   ├── GridPulse.Application/         # Application services
│   ├── GridPulse.Infrastructure/      # Data access
│   ├── GridPulse.WebApi/              # REST API
│   ├── GridPulse.Web/                 # Blazor UI
│   │   ├── GridPulse.Web/             # Server host
│   │   └── GridPulse.Web.Client/      # WASM client
│   ├── GridPulse.AppHost/             # Aspire orchestration
│   └── GridPulse.Tests.Unit/          # Unit tests
├── docs/                              # Documentation
├── .github/                           # GitHub workflows
├── GridPulse.sln                      # Solution file
└── README.md                          # Project overview
```

### Dependency Graph

```
AppHost
  ├── WebApi
  │   ├── Application
  │   │   └── Domain
  │   └── Infrastructure
  │       └── Application
  └── Web
      └── (HTTP) → WebApi
```

**Key Rules**:
- Domain has no dependencies
- Application depends only on Domain
- Infrastructure implements Application interfaces
- Web and WebApi are entry points

---

## Development Workflows

### Daily Development

#### 1. Start Aspire Dashboard

```bash
aspire run --project src/GridPulse.AppHost/GridPulse.AppHost.csproj
```

This starts:
- WebApi on `https://localhost:7143`
- Web UI on auto-assigned port
- Aspire dashboard for monitoring

#### 2. Make Code Changes

Edit files in your preferred IDE. Aspire supports hot reload for many changes.

#### 3. Build Incrementally

```bash
# Build specific project
dotnet build src/GridPulse.WebApi/GridPulse.WebApi.csproj

# Build entire solution
dotnet build GridPulse.sln
```

#### 4. Run Tests

```bash
# Run all tests
dotnet test GridPulse.sln

# Run specific test project
dotnet test src/GridPulse.Tests.Unit/GridPulse.Tests.Unit.csproj

# Run with verbose output
dotnet test GridPulse.sln --logger "console;verbosity=detailed"
```

#### 5. Verify Changes

- Check API: `https://localhost:7143/scalar/v1`
- Check UI: Navigate to Web endpoint from Aspire dashboard
- Review logs in Aspire dashboard

---

### Authentication Placeholder & Feature Flag

Authentication is intentionally mocked until the Entra integration lands. Both `GridPulse.WebApi` and `GridPulse.Web` read a shared configuration block:

```json
"Authentication": {
    "Provider": "Mock",
    "Mock": {
        "DisplayName": "Auto Operator",
        "Roles": ["operator", "dispatcher"],
        "Claims": {
            "tenant": "default",
            "scope": "ticketing"
        }
    }
}
```

- `Provider` is a feature flag. It must remain `Mock` until Entra wiring replaces `MockUserContext`; any other value throws at startup so we do not accidentally ship without real auth.
- Update the `Mock` payload to impersonate different personas (e.g., dispatcher-only) when demoing. Both hosts read the same settings so UI and API stay in sync.
- **TODO**: replace `MockUserContext` with an Entra-backed provider and allow `Provider` to switch between `Mock` and `Entra` without touching endpoints or components.

Document changes whenever you alter roles/claims so QA scripts and Playwright scenarios can adopt the new expectations.

---

### Adding New Features

#### Domain Entity

1. Create entity in `GridPulse.Domain/Entities/`
2. Use immutable properties (`init` setters)
3. Add to `_Imports.razor` if used in UI

**Example**:
```csharp
namespace GridPulse.Domain.Entities;

public sealed class Equipment
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DateTimeOffset InstalledAt { get; init; }
}
```

#### Application Service

1. Create interface in `GridPulse.Application/Abstractions/`
2. Create implementation in `GridPulse.Application/Services/`
3. Register in `ServiceCollectionExtensions.cs`

**Example**:
```csharp
// IEquipmentReadService.cs
public interface IEquipmentReadService
{
    Task<IReadOnlyCollection<Equipment>> GetAllAsync(
        CancellationToken cancellationToken = default);
}

// EquipmentReadService.cs
internal sealed class EquipmentReadService(IEquipmentRepository repository) 
    : IEquipmentReadService
{
    public Task<IReadOnlyCollection<Equipment>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return repository.GetAllAsync(cancellationToken);
    }
}

// ServiceCollectionExtensions.cs
public static IServiceCollection AddApplicationServices(
    this IServiceCollection services)
{
    services.AddScoped<IOutageReadService, OutageReadService>();
    services.AddScoped<IEquipmentReadService, EquipmentReadService>(); // Add this
    return services;
}
```

#### API Endpoint

1. Add endpoint group in `GridPulse.WebApi/Program.cs`
2. Use Minimal API pattern
3. Tag for OpenAPI documentation

**Example**:
```csharp
var equipmentGroup = app.MapGroup("/api/equipment")
    .WithTags("Equipment");

equipmentGroup.MapGet(
    "/",
    async (IEquipmentReadService service, CancellationToken cancellationToken) =>
    {
        var items = await service.GetAllAsync(cancellationToken);
        return Results.Ok(items);
    })
    .WithName("GetAllEquipment");
```

#### Blazor Component

1. Create in appropriate folder (Atoms, Molecules, Organisms)
2. Use `[Parameter]` for inputs
3. Follow atomic design principles

**Example**:
```razor
@* Components/Atoms/EquipmentBadge.razor *@
<span class="badge badge-@GetBadgeClass()">
    @Equipment.Type
</span>

@code {
    [Parameter] public Equipment Equipment { get; set; } = null!;
    
    private string GetBadgeClass() => Equipment.Type switch
    {
        "Transformer" => "primary",
        "PowerLine" => "warning",
        _ => "secondary"
    };
}
```

### Sample Data & Database Seeding

- **Source of truth**: CSV files live under `sample-data/outages/`. `outages.csv` describes outage headers and `outage-events.csv` tracks each timeline entry.
- **Copy to output**: `Directory.Build.props` links every file under `sample-data/` into each project and copies them into `bin/<tfm>/sample-data` so runtime hosts can always resolve them.
- **Configuration**: `SampleData` settings in `src/GridPulse.WebApi/appsettings*.json` control the relative folder and file names. The defaults expect `sample-data` beside the repo root; point `RootPath` at an absolute path to override.
- **Seeding workflow**: At startup the API migrates the database and runs each `IDataSeeder`. The CSV seeder inserts rows only when `outages` is empty so real data is never overwritten.
- **Extending the dataset**:
    1. Add rows to the CSVs or create additional CSVs under `sample-data/<category>/`.
    2. Reference new files from a dedicated seeder (implement `IDataSeeder`) or update the existing CSV files.
    3. Drop/truncate the target tables (or rebuild the database) before restarting the API to replay the sample data.
- **Validation tips**: Keep GUIDs stable between related files, store timestamps as ISO 8601 strings, and use enum names (`Reported`, `CrewDispatched`, etc.) for hassle-free parsing.

---

### Aspire CLI Commands

#### Run Application

```bash
# Run with defaults
aspire run

# Run specific project
aspire run --project src/GridPulse.AppHost/GridPulse.AppHost.csproj

# Pass arguments to application
aspire run -- --urls http://localhost:5000
```

#### Add Integration

```bash
# Interactive mode
aspire add

# Add specific integration
aspire add redis
aspire add postgres
aspire add sqlserver

# Add to specific project
aspire add redis --project src/GridPulse.AppHost/GridPulse.AppHost.csproj
```

#### Publish for Deployment

```bash
# Publish to default output
aspire publish

# Publish to specific path
aspire publish -o ./publish

# Publish for Production environment
aspire publish -e Production

# Publish specific project
aspire publish --project src/GridPulse.AppHost/GridPulse.AppHost.csproj
```

#### Update

```bash
# Update integrations in project
aspire update

# Update Aspire CLI itself
aspire update --self
```

#### Configuration

```bash
# List all configuration
aspire config list

# Get specific value
aspire config get <key>

# Set value
aspire config set <key> <value>
```

---

## Building and Testing

### Build Commands

```bash
# Clean solution
dotnet clean GridPulse.sln

# Restore packages
dotnet restore GridPulse.sln

# Build in Debug (default)
dotnet build GridPulse.sln

# Build in Release
dotnet build GridPulse.sln --configuration Release

# Build specific project
dotnet build src/GridPulse.WebApi/GridPulse.WebApi.csproj

# Build with detailed output
dotnet build GridPulse.sln --verbosity detailed
```

### Test Commands

```bash
# Run all tests
dotnet test GridPulse.sln

# Run with code coverage
dotnet test GridPulse.sln --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~OutageReadServiceTests.GetRecentAsync"

# Run tests in parallel
dotnet test GridPulse.sln --parallel

# Generate test report
dotnet test GridPulse.sln --logger "trx;LogFileName=test-results.trx"
```

### Test Organization

```
GridPulse.Tests.Unit/
├── Domain/              # Domain entity tests
├── Application/         # Service tests
│   └── Services/
└── Infrastructure/      # Repository tests
    └── Repositories/
```

### Writing Tests

**Example xUnit Test**:
```csharp
public class OutageReadServiceTests
{
    [Fact]
    public async Task GetRecentAsync_ReturnsOutages()
    {
        // Arrange
        var repository = new InMemoryOutageRepository();
        var service = new OutageReadService(repository);
        
        // Act
        var result = await service.GetRecentAsync();
        
        // Assert
        Assert.NotEmpty(result);
    }
}
```

---

## Running the Application

### Local Development

#### Method 1: Aspire CLI (Recommended)

```bash
aspire run --project src/GridPulse.AppHost/GridPulse.AppHost.csproj
```

**Benefits**:
- Orchestrates all services
- Provides dashboard
- Enables service discovery
- Manages configuration

#### Method 2: Direct dotnet run

```bash
# Terminal 1: Run API
cd src/GridPulse.WebApi
dotnet run

# Terminal 2: Run Web UI
cd src/GridPulse.Web/GridPulse.Web
dotnet run
```

**Note**: Update `appsettings.json` in Web project with correct API base URL.

### Accessing the Application

#### Aspire Dashboard
- Opens automatically when running via `aspire run`
- URL: Auto-generated (check terminal output)
- Shows all services and their endpoints

#### Web API
- Base URL: `https://localhost:7143`
- Health Check: `https://localhost:7143/api/health`
- Scalar Docs: `https://localhost:7143/scalar/v1`
- OpenAPI: `https://localhost:7143/openapi/v1.json`

#### Web UI
- URL: Shown in Aspire dashboard
- Default route: `/` (Dashboard page)

---

## Debugging

### Visual Studio 2022

1. Set `GridPulse.AppHost` as startup project
2. Press F5 to start debugging
3. Breakpoints work across all projects
4. Use Aspire dashboard to monitor services

### Visual Studio Code

1. Open workspace in VS Code
2. Install C# Dev Kit extension
3. Use `.vscode/launch.json` configuration:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch Aspire",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/GridPulse.AppHost/bin/Debug/net10.0/GridPulse.AppHost.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/GridPulse.AppHost",
      "stopAtEntry": false
    }
  ]
}
```

### Rider

1. Right-click `GridPulse.AppHost` project
2. Select "Debug"
3. Breakpoints work across all projects

### Debugging Tips

- **Attach to Process**: Use "Attach to Process" for running services
- **Aspire Logs**: Check logs in Aspire dashboard for diagnostics
- **API Testing**: Use Scalar UI for quick API testing
- **Network Traffic**: Use browser DevTools to inspect API calls

---

## Code Standards

### C# Conventions

#### Naming
- **PascalCase**: Classes, methods, properties, public fields
- **camelCase**: Parameters, local variables, private fields
- **_camelCase**: Private fields (with underscore prefix) - avoid if possible

#### Style
- Use `var` for obvious types
- Prefer expression-bodied members
- Use `async`/`await` consistently
- File-scoped namespaces

**Example**:
```csharp
namespace GridPulse.Application.Services;

public sealed class OutageReadService(IOutageRepository repository) : IOutageReadService
{
    public async Task<IReadOnlyCollection<OutageSummary>> GetRecentAsync(
        CancellationToken cancellationToken = default)
    {
        var outages = await repository.GetRecentAsync(25, cancellationToken);
        return outages.Select(MapToSummary).ToArray();
    }
}
```

### Domain Model Standards

- Immutable entities (`init` setters)
- Sealed classes
- `IReadOnlyCollection<T>` for collections
- No infrastructure dependencies
- Rich business logic where appropriate

### API Standards

- Minimal API pattern
- Explicit endpoint groups
- Tagged for OpenAPI
- Return `Results.*` types
- Async all the way

### Blazor Standards

- Atomic design hierarchy
- `[Parameter]` for inputs
- `EventCallback` for events
- Loading and empty states
- Error boundaries

---

## Git Workflow

### Branching Strategy

```
main (production)
  └── develop (integration)
       ├── feature/add-customer-api
       ├── feature/usage-dashboard
       └── bugfix/outage-status-update
```

### Commit Messages

Follow conventional commits:

```
type(scope): subject

body (optional)

footer (optional)
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Formatting
- `refactor`: Code restructuring
- `test`: Adding tests
- `chore`: Maintenance

**Examples**:
```
feat(api): add customer endpoints
fix(ui): resolve status chip color issue
docs: update architecture documentation
```

### Pull Request Process

1. Create feature branch
2. Make changes
3. Run tests: `dotnet test`
4. Build: `dotnet build`
5. Push and create PR
6. Wait for review and CI checks
7. Merge to develop

---

## Troubleshooting

### Build Errors

#### "Project not found"
```bash
# Restore packages
dotnet restore GridPulse.sln
```

#### "SDK not found"
```bash
# Check .NET version
dotnet --version

# Install .NET 10 SDK
```

### Runtime Errors

#### "Port already in use"
- Check for running instances
- Change port in `AppHost.cs` or `appsettings.json`

#### "Cannot connect to API"
- Verify API is running: `https://localhost:7143/api/health`
- Check `Api:BaseAddress` in Web UI `appsettings.json`
- Review CORS settings in API `Program.cs`

### Aspire Issues

#### "Aspire CLI not found"
```bash
# Install/update Aspire CLI
dotnet workload install aspire
aspire update --self
```

#### "Dashboard won't open"
- Check firewall settings
- Try different browser
- Review Aspire logs

### Performance Issues

- Clear `bin/` and `obj/` folders
- Restart Aspire dashboard
- Check system resources
- Review Aspire dashboard logs

---

## Additional Resources

### Official Documentation
- [.NET Documentation](https://learn.microsoft.com/dotnet/)
- [Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)

### Project Documentation
- [Architecture](architecture.md)
- [Features](features.md)
- [Domain Model](domain-model.md)
- [API Reference](api.md)
- [UI Components](ui-components.md)

### Tools
- [Scalar API Documentation](https://github.com/scalar/scalar)
- [Radzen Blazor Components](https://blazor.radzen.com/)
- [xUnit Testing](https://xunit.net/)

---

## Getting Help

- Review existing documentation
- Check GitHub issues
- Ask team members
- Consult official .NET/Aspire docs
