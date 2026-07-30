import type { Page } from '@playwright/test'

// Every test registers its own throwaway account — avoids collisions between
// test runs (the backend/Mongo state persists between runs, unlike a typical
// mocked frontend test) without needing a database reset step.
export function uniqueEmail(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 10000)}@example.com`
}

export async function registerAndLogIn(page: Page, displayName: string): Promise<{ email: string; password: string }> {
  const email = uniqueEmail('e2e')
  const password = 'TestPassword123!'

  await page.goto('/register')
  await page.getByLabel('Display name').fill(displayName)
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Create account' }).click()

  await page.waitForURL('**/teams')

  return { email, password }
}
