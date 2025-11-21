import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.GRIDPULSE_WEB_BASEURL ?? 'https://localhost:7210';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  timeout: 90_000,
  expect: {
    timeout: 10_000,
  },
  retries: process.env.CI ? 1 : 0,
  reporter: [
    ['list'],
    ['html', { outputFolder: 'artifacts/html-report', open: 'never' }]
  ],
  use: {
    baseURL,
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
    ignoreHTTPSErrors: true
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] }
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] }
    }
  ],
  webServer: process.env.GRIDPULSE_SKIP_WEBSERVER === '1'
    ? undefined
    : {
        command: 'aspire run --project ./src/GridPulse.AppHost/GridPulse.AppHost.csproj',
        url: baseURL,
        reuseExistingServer: true,
        stdout: 'pipe',
        stderr: 'pipe',
        timeout: 180_000
      }
});
