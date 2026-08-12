import { describe, expect, it } from 'vitest'
import { avatarColor } from '../avatarColor'

describe('avatarColor', () => {
  it('returns the same color for the same user id every time', () => {
    const id = 'user-123'
    expect(avatarColor(id)).toBe(avatarColor(id))
  })

  it('returns a valid hex color', () => {
    expect(avatarColor('user-123')).toMatch(/^#[0-9A-Fa-f]{6}$/)
  })

  it('spreads different ids across more than one color', () => {
    const ids = Array.from({ length: 20 }, (_, i) => `user-${i}`)
    const colors = new Set(ids.map(avatarColor))
    expect(colors.size).toBeGreaterThan(1)
  })
})
