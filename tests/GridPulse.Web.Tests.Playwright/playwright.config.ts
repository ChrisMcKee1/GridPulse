import { defineConfig, devices } from '@playwright/test';
import fs from 'fs';
import path from 'path';

const repoRoot = path.resolve(__dirname, '..', '..');
const launchSettingsSegments = {
  web: ['src', 'GridPulse.Web', 'GridPulse.Web'],
  webApi: ['src', 'GridPulse.WebApi']
};

const resolveHttpsFromLaunchSettings = (segments: string[]): string | undefined => {
  const launchSettingsPath = path.resolve(repoRoot, ...segments, 'Properties', 'launchSettings.json');

  try {
    const rawBuffer = fs.readFileSync(launchSettingsPath);
    const sanitized = rawBuffer.toString('utf-8').replace(/^\uFEFF/, '');
    const data = JSON.parse(sanitized) as { profiles?: Record<string, { applicationUrl?: string }> };
    const profiles = data.profiles ?? {};

    for (const profile of Object.values(profiles)) {
      if (!profile?.applicationUrl) {
        continue;
      }

      const urls = profile.applicationUrl
        .split(';')
        .map(url => url.trim())
        .filter(Boolean);

      const httpsUrl = urls.find(url => url.startsWith('https://'));
      if (httpsUrl) {
        return httpsUrl;
      }
    }
  }
  catch (error) {
    console.warn(`Unable to parse launchSettings.json at ${launchSettingsPath}: ${(error as Error).message}`);
  }

  return undefined;
};

const fallbackWebBaseUrl = resolveHttpsFromLaunchSettings(launchSettingsSegments.web) ?? 'https://localhost:7210';
const baseURL = process.env.GRIDPULSE_WEB_BASEURL ?? fallbackWebBaseUrl;
process.env.GRIDPULSE_WEB_BASEURL = baseURL;

if (!process.env.GRIDPULSE_API_BASEURL) {
  const fallbackApiUrl = resolveHttpsFromLaunchSettings(launchSettingsSegments.webApi);
  if (fallbackApiUrl) {
    process.env.GRIDPULSE_API_BASEURL = fallbackApiUrl;
  }
}
const appHostProjectPath = './src/GridPulse.AppHost/GridPulse.AppHost.csproj';

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
      use: { 
        ...devices['Desktop Firefox'],
        // Firefox needs more time for Interactive Auto prerendering
        actionTimeout: 25_000,
        navigationTimeout: 45_000
      }
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] }
    }
  ],
  webServer: process.env.GRIDPULSE_SKIP_WEBSERVER === '1'
    ? undefined
    : {
        command: `aspire run --project ${appHostProjectPath}`,
        url: baseURL,
        ignoreHTTPSErrors: true,
        reuseExistingServer: true,
        stdout: 'pipe',
        stderr: 'pipe',
        timeout: 180_000,
        cwd: repoRoot
      }
});
