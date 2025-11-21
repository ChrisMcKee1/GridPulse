---
description: ".NET Aspire CLI Cheat Sheet - Quick reference for common Aspire CLI commands and workflows."
applyTo: '**'
---
# .NET Aspire CLI Cheat Sheet

## Quick Reference for Aspire CLI Commands

---

## Installation & Version

```powershell
# Check Aspire CLI version
aspire --version

# Update Aspire CLI to latest version
aspire update --self

# Update to specific quality level (stable, staging, daily)
aspire update --self --quality stable
```

---

## Creating New Projects

### New Aspire Projects

```powershell
# Create new Blazor & Minimal API starter
aspire new aspire-starter -n MyApp -o ./MyApp

# Create new React (Vite) & FastAPI starter (Python)
aspire new aspire-py-starter -n MyPythonApp -o ./MyPythonApp

# Create empty AppHost (single-file)
aspire new aspire-apphost-singlefile -n MyAppHost -o ./MyAppHost

# Options:
# -n, --name <name>        Project name
# -o, --output <output>    Output path
# -s, --source <source>    NuGet source for templates
# -v, --version <version>  Template version to use
```

### Initialize Aspire in Existing Solution

```powershell
# Initialize Aspire support in current directory
aspire init

# With specific template version
aspire init -v 9.0.0

# With custom NuGet source
aspire init -s https://api.nuget.org/v3/index.json
```

---

## Running Applications

```powershell
# Run Aspire AppHost in development mode
aspire run

# Run specific project
aspire run --project ./MyApp.AppHost/MyApp.AppHost.csproj

# Pass additional arguments to the application
aspire run -- --urls http://localhost:5000
```

---

## Adding Integrations

```powershell
# Add integration (interactive mode)
aspire add

# Add specific integration
aspire add redis
aspire add postgres
aspire add mongodb
aspire add rabbitmq
aspire add sqlserver

# Add to specific project
aspire add redis --project ./MyApp.AppHost/MyApp.AppHost.csproj

# Add specific version
aspire add redis -v 9.0.0

# With custom NuGet source
aspire add redis -s https://api.nuget.org/v3/index.json
```

### Common Integrations

| Integration | Command |
|------------|---------|
| Redis | `aspire add redis` |
| PostgreSQL | `aspire add postgres` |
| SQL Server | `aspire add sqlserver` |
| MongoDB | `aspire add mongodb` |
| RabbitMQ | `aspire add rabbitmq` |
| Azure Storage | `aspire add azurestorage` |
| Cosmos DB | `aspire add cosmosdb` |

---

## Publishing & Deployment

### Publish (Generate Artifacts)

```powershell
# Publish to default output (./aspire-output)
aspire publish

# Publish to specific output path
aspire publish -o ./publish

# Publish specific project
aspire publish --project ./MyApp.AppHost/MyApp.AppHost.csproj

# Set environment
aspire publish -e Production

# Set log level (trace, debug, information, warning, error, critical)
aspire publish --log-level debug

# Include exception details in logs
aspire publish --include-exception-details

# Pass additional arguments
aspire publish -- --configuration Release
```

### Deploy (Preview)

```powershell
# Deploy to defined targets
aspire deploy

# Deploy specific project
aspire deploy --project ./MyApp.AppHost/MyApp.AppHost.csproj

# Deploy to specific output path
aspire deploy -o ./deploy

# Set environment
aspire deploy -e Staging

# Clear deployment cache
aspire deploy --clear-cache

# Set log level
aspire deploy --log-level information
```

---

## Configuration Management

```powershell
# List all configuration values
aspire config list

# Get specific configuration value
aspire config get <key>

# Set configuration value
aspire config set <key> <value>

# Delete configuration value
aspire config delete <key>
```

---

## Update & Maintenance

```powershell
# Update integrations in project
aspire update

# Update specific project's integrations
aspire update --project ./MyApp.AppHost/MyApp.AppHost.csproj

# Update Aspire CLI itself
aspire update --self
```

---

## Cache Management

```powershell
# Manage disk cache for CLI operations
aspire cache

# (Use --help to see cache-specific commands)
aspire cache --help
```

---

## Pipeline Operations (Preview)

```powershell
# Execute specific pipeline step and dependencies
aspire do <step>

# (Use --help for more details)
aspire do --help
```

---

## Global Options

Available for all commands:

```powershell
-d, --debug             # Enable debug logging to console
--non-interactive       # Disable interactive prompts and spinners
--wait-for-debugger     # Wait for debugger to attach before executing
-?, -h, --help          # Show help and usage information
```

---

## .NET Templates (via dotnet new)

If you prefer using `dotnet new` instead of `aspire new`:

```powershell
# List all Aspire templates
dotnet new list | Select-String aspire

# Create projects with dotnet new
dotnet new aspire                      # Aspire Empty App
dotnet new aspire-starter              # Aspire Starter App
dotnet new aspire-apphost              # Aspire AppHost
dotnet new aspire-servicedefaults      # Aspire Service Defaults
dotnet new aspire-mstest               # Aspire Test Project (MSTest)
dotnet new aspire-xunit                # Aspire Test Project (xUnit)
dotnet new aspire-nunit                # Aspire Test Project (NUnit)
```

---

## Common Workflows

### 1. Create New Aspire Application

```powershell
# Create starter application
aspire new aspire-starter -n MyApp -o ./MyApp
cd MyApp
aspire run
```

### 2. Add Integration to Existing App

```powershell
# Navigate to solution directory
cd MyApp
aspire add redis
aspire run
```

### 3. Initialize Aspire in Existing Solution

```powershell
# Navigate to solution directory
cd MyExistingSolution
aspire init
aspire run
```

### 4. Publish for Deployment

```powershell
# Generate deployment artifacts
aspire publish -o ./publish -e Production
```

### 5. Update Everything

```powershell
# Update project integrations
aspire update

# Update CLI itself
aspire update --self
```

---

## Tips & Best Practices

1. **Use `--non-interactive` in CI/CD**: For automated pipelines, always use `--non-interactive` to avoid hanging on prompts
2. **Project Path**: When working with multi-project solutions, use `--project` to specify the AppHost project
3. **Environment Variables**: Use `-e` or `--environment` to switch between Development, Staging, and Production
4. **Debug Mode**: Use `-d` or `--debug` for troubleshooting CLI issues
5. **Version Pinning**: Use `-v` flag when adding integrations to ensure consistent versions across environments

---

## Getting Help

```powershell
# General help
aspire --help

# Command-specific help
aspire <command> --help

# Examples:
aspire new --help
aspire add --help
aspire run --help
aspire publish --help
```

---

## Version Information

This cheat sheet is based on Aspire CLI version 13.0.0.0

Check your version: `aspire --version`

---

## Resources

- Official Documentation: https://learn.microsoft.com/dotnet/aspire/
- GitHub Repository: https://github.com/dotnet/aspire
- NuGet Packages: https://www.nuget.org/packages?q=Aspire

---

*Last Updated: 2025-11-17*
