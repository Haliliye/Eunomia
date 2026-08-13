import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import StatusBadge from '../StatusBadge'

describe('StatusBadge', () => {
  it('shows the known label and a colored class for a default column key', () => {
    render(<StatusBadge status="ToDo" />)

    const badge = screen.getByText('To Do')
    expect(badge.className).toContain('badge-status-todo')
  })

  it('falls back to the raw key with a neutral class for an unrecognized status', () => {
    render(<StatusBadge status="Custom_ab12cd34" />)

    // No known label for a custom column key — the raw key is shown as-is
    // rather than something misleading.
    const badge = screen.getByText('Custom_ab12cd34')
    expect(badge.className).toBe('badge')
  })

  it('prefers an explicitly passed label over the built-in lookup', () => {
    // This is how callers with team.columns loaded show a renamed default
    // column's real display name instead of the raw "To Do" fallback.
    render(<StatusBadge status="ToDo" label="Backlog" />)

    expect(screen.getByText('Backlog')).toBeInTheDocument()
    expect(screen.queryByText('To Do')).not.toBeInTheDocument()
  })
})
