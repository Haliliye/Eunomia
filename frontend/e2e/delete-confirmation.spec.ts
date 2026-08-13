import { test, expect } from '@playwright/test'
import { registerAndLogIn } from './helpers'

test.describe('Delete confirmation (in-app dialog, not window.confirm)', () => {
  test('cancelling the delete dialog keeps the story; confirming removes it', async ({ page }) => {
    await registerAndLogIn(page, 'Delete Confirm E2E User')

    const teamName = `E2E Delete Team ${Date.now()}`
    await page.getByRole('button', { name: '+ New Team' }).click()
    await page.getByLabel('Name').fill(teamName)
    await page.getByRole('button', { name: 'Create team' }).click()
    await page.getByText(teamName).click()

    await page.getByRole('link', { name: 'Backlog' }).click()
    await expect(page).toHaveURL(/\/backlog$/)

    const storyTitle = `E2E Delete Story ${Date.now()}`
    await page.getByRole('button', { name: '+ Create story' }).click()
    await page.getByLabel('Title').fill(storyTitle)
    await page.getByRole('button', { name: 'Create story' }).click()
    await expect(page.getByText(storyTitle)).toBeVisible()

    const row = page.locator('.backlog-row', { hasText: storyTitle })
    await row.hover() // the row's actions are hover/focus-revealed, see .backlog-row-actions
    await row.getByRole('button', { name: /delete/i }).click()

    // The dialog is a real in-app element (role="alertdialog"), not a native
    // window.confirm() — Playwright would need page.on('dialog') for that,
    // and this deliberately doesn't register one, so a leftover native
    // confirm() call here would hang the test rather than pass it.
    const dialog = page.getByRole('alertdialog')
    await expect(dialog).toBeVisible()
    await expect(dialog.getByText(`Delete "${storyTitle}"?`)).toBeVisible()

    // Cancel first — the story must still be there afterward.
    await dialog.getByRole('button', { name: 'Cancel' }).click()
    await expect(dialog).not.toBeVisible()
    await expect(page.getByText(storyTitle)).toBeVisible()

    // Now actually delete it.
    await row.hover()
    await row.getByRole('button', { name: /delete/i }).click()
    await page.getByRole('alertdialog').getByRole('button', { name: 'Delete' }).click()

    await expect(page.getByText(storyTitle)).not.toBeVisible()
  })
})
