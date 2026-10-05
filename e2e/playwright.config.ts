import { defineConfig, devices } from '@playwright/test';

// End-to-end tests run against the real stack (nginx + API + PostgreSQL) started
// by the CI workflow `.github/workflows/e2e.yml` (or `npm run docker:dev` locally).
// The base URL and API URL are overridable so the same suite works locally and in CI.
const baseURL = process.env.E2E_BASE_URL ?? 'http://127.0.0.1:4200';

export default defineConfig({
  testDir: './tests',
  // The suite mutates shared backend state (users/motorcycles), so run serially.
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
