import { test, expect } from '@playwright/test'
import { registerAndLogIn } from './helpers'

test.describe('Team and user story lifecycle', () => {
  test('create a team, create a story, move it through the workflow, see it on the board', async ({ page }) => {
    await registerAndLogIn(page, 'Team Story E2E User')

    // --- Create a team ---
    const teamName = `E2E Team ${Date.now()}`
    await page.getByRole('button', { name: '+ New Team' }).click()
    await page.getByLabel('Name').fill(teamName)
    await page.getByRole('button', { name: 'Create team' }).click()

    await expect(page.getByText(teamName)).toBeVisible()
    await page.getByText(teamName).click()

    // Shell redirects /teams/:id -> /teams/:id/summary by default.
    await expect(page).toHaveURL(/\/summary$/)

    // --- Create a story from the Backlog tab ---
    await page.getByRole('link', { name: 'Backlog' }).click()
    await expect(page).toHaveURL(/\/backlog$/)

    const storyTitle = `E2E Story ${Date.now()}`
    await page.getByRole('button', { name: '+ Create story' }).click()
    await page.getByLabel('Title').fill(storyTitle)
    await page.getByRole('button', { name: 'Create story' }).click()

    await expect(page.getByText(storyTitle)).toBeVisible()

    // --- Move it through the workflow via the inline status select ---
    const row = page.locator('.backlog-row', { hasText: storyTitle })
    await row.getByLabel('Status').selectOption('Analyze')
    await expect(row.getByLabel('Status')).toHaveValue('Analyze')

    // --- Confirm it shows up in the matching column on the Board tab ---
    await page.getByRole('link', { name: 'Board' }).click()
    await expect(page).toHaveURL(/\/board$/)

    const analyzeColumn = page.locator('.board-column', { hasText: 'Analyze' })
    await expect(analyzeColumn.getByText(storyTitle)).toBeVisible()
  })
})
