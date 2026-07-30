import { defineConfig, devices } from '@playwright/test'

// Requires the frontend AND backend both running (see README) — Playwright
// doesn't spin up the backend/Mongo for you. Run `npm run dev` in one
// terminal, then `npm run test:e2e` in another.
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false, // tests share a real backend/DB — avoid cross-test interference
  retries: 0,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
})
