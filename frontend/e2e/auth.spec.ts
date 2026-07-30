import { test, expect } from '@playwright/test'
import { uniqueEmail } from './helpers'

test.describe('Registration and login', () => {
  test('a new user can register, land on My Teams, log out, and log back in', async ({ page }) => {
    const email = uniqueEmail('auth')
    const password = 'TestPassword123!'

    await page.goto('/register')
    await page.getByLabel('Display name').fill('E2E Test User')
    await page.getByLabel('Email').fill(email)
    await page.getByLabel('Password').fill(password)
    await page.getByRole('button', { name: 'Create account' }).click()

    await expect(page).toHaveURL(/\/teams$/)
    await expect(page.getByRole('heading', { name: 'My Teams' })).toBeVisible()

    // Log out — should land back on the login page.
    await page.getByRole('button', { name: 'Log out' }).click()
    await expect(page).toHaveURL(/\/login$/)

    // Log back in with the same credentials.
    await page.getByLabel('Email').fill(email)
    await page.getByLabel('Password').fill(password)
    await page.getByRole('button', { name: 'Log in' }).click()

    await expect(page).toHaveURL(/\/teams$/)
  })

  test('logging in with a wrong password shows an error instead of proceeding', async ({ page }) => {
    const email = uniqueEmail('wrongpass')
    const password = 'TestPassword123!'

    // Register first so the account genuinely exists.
    await page.goto('/register')
    await page.getByLabel('Display name').fill('Wrong Password User')
    await page.getByLabel('Email').fill(email)
    await page.getByLabel('Password').fill(password)
    await page.getByRole('button', { name: 'Create account' }).click()
    await expect(page).toHaveURL(/\/teams$/)

    await page.getByRole('button', { name: 'Log out' }).click()

    await page.getByLabel('Email').fill(email)
    await page.getByLabel('Password').fill('DefinitelyWrongPassword!')
    await page.getByRole('button', { name: 'Log in' }).click()

    // Should stay on the login page with an error, not silently proceed.
    await expect(page).toHaveURL(/\/login$/)
    await expect(page.locator('.field-error')).toBeVisible()
  })
})
