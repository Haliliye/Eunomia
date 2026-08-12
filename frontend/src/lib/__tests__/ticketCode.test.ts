import { describe, expect, it } from 'vitest'
import { ticketCode } from '../ticketCode'

describe('ticketCode', () => {
  it('uses up to the first three word-initials of the team name, uppercased', () => {
    expect(ticketCode('Platform Engineering', 'abc123-def456')).toMatch(/^PE-/)
    expect(ticketCode('Growth', 'abc123-def456')).toMatch(/^G-/)
    expect(ticketCode('Search Infra Team', 'abc123-def456')).toMatch(/^SIT-/)
  })

  it('falls back to "GEN" when the team name has no usable initials', () => {
    expect(ticketCode('   ', 'abc123-def456')).toMatch(/^GEN-/)
  })

  it('derives the suffix from the story id with dashes stripped, uppercased, max 6 chars', () => {
    const code = ticketCode('Platform', 'ab-cd-ef-gh-ij')
    expect(code).toBe('P-ABCDEF')
  })

  it('is deterministic for the same inputs', () => {
    expect(ticketCode('Platform Team', 'story-1')).toBe(ticketCode('Platform Team', 'story-1'))
  })
})
