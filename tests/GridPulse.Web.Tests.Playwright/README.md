# GridPulse Playwright Tests

This project hosts the end-to-end UI tests requested in `specs/001-ticket-dispatch/tasks.md`.

## Why Playwright

- Microsoft recommends using Playwright for cross-browser end-to-end testing (see [Set up test automation with Playwright](https://learn.microsoft.com/en-us/dynamics365/guidance/resources/test-automation-setup)).
- Microsoft Playwright Testing guidance outlines how to keep `.runsettings` and cloud execution ready for CI ([Quickstart: Set up continuous end-to-end testing](https://learn.microsoft.com/en-us/azure/playwright-testing/quickstart-automate-end-to-end-testing)).
- Tests assume the Aspire AppHost is responsible for orchestrating dependencies. You can let Playwright start it for you or run `aspire run --project src/GridPulse.AppHost/GridPulse.AppHost.csproj` ahead of time (see [Aspire quickstart testing section](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-your-first-aspire-app#test-the-app-locally)).

## Prerequisites

1. Node.js 18 LTS+
2. `aspire` CLI available on PATH
3. Certificates trusted locally (`dotnet dev-certs https --trust`)
4. Run `npm install` from this directory to restore dependencies and browsers:

```powershell
npm install
echo "Installing browsers"
npx playwright install --with-deps
```

## Running the tests

```powershell
cd tests/GridPulse.Web.Tests.Playwright
npm test
```

Default config tries to launch the full Aspire AppHost using the Playwright `webServer` hook. If you already have the stack running, skip the hook by exporting `GRIDPULSE_SKIP_WEBSERVER=1`.

Useful overrides:

```powershell
# Point to a different base URL
$env:GRIDPULSE_WEB_BASEURL = "https://localhost:5500"

# Keep an existing server alive
$env:GRIDPULSE_SKIP_WEBSERVER = "1"

npm test
```

Artifacts land under `artifacts/html-report`, `playwright-report/`, and `test-results/`.
