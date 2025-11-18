# GridPulse

GridPulse is an Aspire-orchestrated .NET 9/10 solution for the outage & usage portal described in the PRD. The repo currently contains the base Clean Architecture scaffold wired for Aspire 13 ([what's new](https://aspire.dev/whats-new/aspire-13/)), Blazor Interactive Auto UI, and a minimal API layer backed by seed data.

## Project layout

```text
src/
  GridPulse.Domain            // Entities + enums
  GridPulse.Application       // Application services + abstractions
  GridPulse.Infrastructure    // Infrastructure services (temporary in-memory repo)
  GridPulse.WebApi            // Minimal API surface for outages
  GridPulse.Web               // Blazor Web App (Interactive Auto) + WASM client
  GridPulse.AppHost           // Aspire AppHost orchestrating API + Web
  GridPulse.Tests.Unit        // xUnit placeholder tests
```

## Prerequisites

- .NET SDK 10.0.100-preview (ships with Aspire 13 templates — see the [what's new notes](https://aspire.dev/whats-new/aspire-13/))
- Aspire CLI (pre-installed per environment notes; verify with `aspire --version`)

## Local development

### Quality gates (dotnet CLI)

```powershell
cd "C:\Users\chrismckee\Downloads\DemoGH\Zava Power"
dotnet build GridPulse.sln
dotnet test GridPulse.sln
```

### Run the distributed app (Aspire CLI)

```powershell
cd "C:\Users\chrismckee\Downloads\DemoGH\Zava Power"
aspire run --project src\GridPulse.AppHost\AppHost.cs
```

The AppHost maps the API at `https://localhost:7143` and the Blazor Web App with external HTTP endpoints. The Blazor UI consumes the API through a typed `GridPulseApiClient` whose base address defaults to the same port (override via `Api:BaseAddress` in `appsettings.*`).

## Aspire CLI workflows

These quick commands mirror the cheat sheet in `.github/instructions/aspire-cli.instructions.md`:

- Bootstrap a fresh environment: `aspire new aspire-starter -n MyApp -o ./MyApp`
- Add integrations (e.g., Redis) to the AppHost: `aspire add redis --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj`
- Publish deployment artifacts: `aspire publish --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj -o ./publish -e Production`
- Deploy or execute pipeline steps: `aspire deploy --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj`
- Keep tooling current: `aspire update --self --quality stable`

## Next steps

1. Replace the in-memory outage repository with an Oracle-backed implementation using `System.Data.Common` and Managed Identity.
2. Add authentication/authorization via Microsoft Entra (B2C for customers, Entra ID for operators) and enforce roles across API/Web.
3. Flesh out the domain/application layers with additional services (usage readings, notification preferences) and expand API endpoints per the PRD.
4. Add Radzen components and richer dashboards to the Blazor UI, plus tests (unit + integration + Playwright).
5. Author IaC + GitHub Actions pipelines to deploy Aspire topologies to Azure Container Apps/App Service.
