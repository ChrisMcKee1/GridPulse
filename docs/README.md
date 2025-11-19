# GridPulse Documentation

Welcome to the GridPulse documentation. This directory contains comprehensive documentation for the GridPulse power outage and usage management system.

## Documentation Index

### Core Documentation
- **[Architecture](architecture.md)** - High-level system architecture and design principles
- **[Features](features.md)** - Currently implemented features and capabilities
- **[Domain Model](domain-model.md)** - Domain entities, enums, and business logic

### Technical Documentation
- **[API Documentation](api.md)** - REST API endpoints and contracts
- **[UI Components](ui-components.md)** - Blazor component library and patterns
- **[Development Guide](development.md)** - Setup, workflows, and best practices

## Quick Links

### Getting Started
1. Read the [Architecture Overview](architecture.md) to understand the system design
2. Review [Features](features.md) to see what's currently implemented
3. Follow the [Development Guide](development.md) to set up your environment

### For Developers
- **Building**: `dotnet build GridPulse.sln`
- **Testing**: `dotnet test GridPulse.sln`
- **Running**: `aspire run --project src/GridPulse.AppHost/GridPulse.AppHost.csproj`

### For API Consumers
- API Base URL (Development): `https://localhost:7143`
- Interactive API Docs (Scalar): `https://localhost:7143/scalar/v1`
- Health Check: `GET /api/health`

## Project Overview

GridPulse is an Aspire-orchestrated .NET 10 solution designed for power outage reporting and usage tracking. Built with Clean Architecture principles, it provides:

- **Real-time outage tracking** with status updates and crew dispatch coordination
- **Customer portal** for viewing outages affecting their service locations
- **Usage monitoring** with historical data and trend analysis
- **Notification preferences** for SMS and email alerts

The system uses:
- **.NET 10** for backend services
- **Aspire 13** for cloud-native orchestration
- **GridPulse.ServiceDefaults** for shared OpenTelemetry, health checks, and HttpClient/service-discovery configuration
- **Blazor Interactive Auto** for the web UI
- **Minimal APIs** for REST endpoints
- **PostgreSQL (Aspire-provisioned)** with EF Core migrations

## Contributing

When contributing to GridPulse:

1. Follow the Clean Architecture patterns established in the codebase
2. Keep domain types immutable (use `init` setters)
3. Use dependency injection for all services
4. Add unit tests for new functionality
5. Update relevant documentation

## Additional Resources

- [Main README](../README.md) - Project setup and quick start
- [Aspire CLI Guide](../.github/instructions/aspire-cli.instructions.md) - Comprehensive CLI reference
- [Copilot Instructions](../.github/copilot-instructions.md) - Development guidelines
