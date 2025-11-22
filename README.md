# GridPulse

GridPulse is an Aspire-orchestrated .NET 9/10 solution for the outage & usage portal described in the PRD. The repo currently contains the base Clean Architecture scaffold wired for Aspire 13 ([what's new](https://aspire.dev/whats-new/aspire-13/)), Blazor Interactive Auto UI, and a minimal API layer backed by seed data.

## Project layout

```text
src/
  GridPulse.Domain            // Entities + enums
  GridPulse.Application       // Application services + abstractions
  GridPulse.Infrastructure    // Infrastructure services (EF Core + PostgreSQL persistence)
  GridPulse.WebApi            // Minimal API surface for outages
  GridPulse.Web               // Blazor Web App (Interactive Auto) + WASM client
  GridPulse.ServiceDefaults   // Shared Aspire defaults (OpenTelemetry, health checks, service discovery)
  GridPulse.AppHost           // Aspire AppHost orchestrating API + Web
  GridPulse.Tests.Unit        // xUnit placeholder tests
```

## Prerequisites

GridPulse leverages .NET Aspire's **amazing time-to-F5** experience — you can clone and run the app in minutes!

### Required

- **[.NET SDK 10.0 Preview](https://dotnet.microsoft.com/download/dotnet/10.0)** (ships with Aspire 13 templates — see the [what's new notes](https://aspire.dev/whats-new/aspire-13/))
  - Download and install from: <https://dotnet.microsoft.com/download/dotnet/10.0>
  - Verify installation: `dotnet --version` (should show 10.0.100-preview or later)
- **[Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)**
  - Installed automatically with .NET 10 SDK
  - Verify installation: `aspire --version`
  - Update to latest: `aspire update --self`
- **An OCI-compliant container runtime** (required for PostgreSQL and other containers):
  - **[Docker Desktop](https://www.docker.com/products/docker-desktop)** (recommended, most widely used)
  - **[Podman](https://podman.io/)** (open-source, daemonless alternative)

> **Note:** Aspire defaults to Docker if both are installed. To use Podman instead, set the environment variable:
>
> ```powershell
> [System.Environment]::SetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME", "podman", "User")
> ```

### Optional

- **Visual Studio 2022** 17.9+ or **Visual Studio Code** with C# Dev Kit
- **JetBrains Rider** with Aspire plugin

## Quick Start (Clone → Run in < 5 minutes!)

### 1. Clone the repository

```powershell
git clone https://github.com/ChrisMcKee1/GridPulse.git
cd GridPulse
```

### 2. Ensure your container runtime is running

Aspire requires containers for PostgreSQL and other services.

**Using Docker Desktop:**

- Start Docker Desktop from your applications menu
- Verify it's running: `docker ps`

**Using Podman:**

- Start Podman: `podman machine start`
- Verify it's running: `podman ps`
- If Aspire should use Podman (when both are installed):

  ```powershell
  [System.Environment]::SetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME", "podman", "User")
  ```

> **Troubleshooting:** If you see "Cannot connect to container runtime" errors, ensure Docker Desktop or Podman is running before executing `aspire run`.

### 3. Run the application

```powershell
aspire run
```

That's it! The Aspire dashboard will open automatically showing:

- **API:** <https://localhost:7143>
- **Web UI:** <https://localhost:7210>
- **Aspire Dashboard:** <http://localhost:15888> (telemetry, logs, traces)

The first run downloads container images (PostgreSQL) and seeds sample outage data automatically.

> **Note:** The `.aspire/settings.json` file in the repo tells Aspire which project to run, so you don't need the `--project` flag!

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
aspire run
```

The AppHost orchestrates the API at `https://localhost:7143` and the Blazor Web App at `https://localhost:7210`. The Blazor UI consumes the API through a typed `GridPulseApiClient` whose base address defaults to the API endpoint (override via `Api:BaseAddress` in `appsettings.*`). Cross-cutting telemetry, health endpoints, and HttpClient resilience live in `GridPulse.ServiceDefaults`; every ASP.NET Core project should reference it and call `builder.AddServiceDefaults()` / `app.MapDefaultEndpoints()`.

## Aspire CLI workflows

These quick commands mirror the cheat sheet in `.github/instructions/aspire-cli.instructions.md`:

- Bootstrap a fresh environment: `aspire new aspire-starter -n MyApp -o ./MyApp`
- Add integrations (e.g., Redis) to the AppHost: `aspire add redis --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj`
- Publish deployment artifacts: `aspire publish --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj -o ./publish -e Production`
- Deploy or execute pipeline steps: `aspire deploy --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj`
- Keep tooling current: `aspire update --self --quality stable`

## Next steps

1. Extend the new PostgreSQL + EF Core stack beyond outages (usage, notifications) and harden connection management for cloud environments.
2. Add authentication/authorization via Microsoft Entra (B2C for customers, Entra ID for operators) and enforce roles across API/Web.
3. Flesh out the domain/application layers with additional services (usage readings, notification preferences) and expand API endpoints per the PRD.
4. Add Radzen components and richer dashboards to the Blazor UI, plus tests (unit + integration + Playwright).
5. Author IaC + GitHub Actions pipelines to deploy Aspire topologies to Azure Container Apps/App Service.
