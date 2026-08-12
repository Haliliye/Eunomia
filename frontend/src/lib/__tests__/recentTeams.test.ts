import { beforeEach, describe, expect, it } from 'vitest'
import { getRecentTeams, recordRecentTeam, removeRecentTeam } from '../recentTeams'

describe('recentTeams', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('returns an empty list for a user with no history', () => {
    expect(getRecentTeams('user-1')).toEqual([])
  })

  it('records a team and returns it most-recent-first', () => {
    recordRecentTeam('user-1', 'team-a', 'Team A')
    recordRecentTeam('user-1', 'team-b', 'Team B')

    expect(getRecentTeams('user-1')).toEqual([
      { id: 'team-b', name: 'Team B' },
      { id: 'team-a', name: 'Team A' },
    ])
  })

  it('moves an already-recorded team to the front instead of duplicating it', () => {
    recordRecentTeam('user-1', 'team-a', 'Team A')
    recordRecentTeam('user-1', 'team-b', 'Team B')
    recordRecentTeam('user-1', 'team-a', 'Team A')

    const recent = getRecentTeams('user-1')
    expect(recent).toHaveLength(2)
    expect(recent[0].id).toBe('team-a')
  })

  it('caps the list at 5 entries', () => {
    for (let i = 0; i < 7; i++) {
      recordRecentTeam('user-1', `team-${i}`, `Team ${i}`)
    }

    expect(getRecentTeams('user-1')).toHaveLength(5)
  })

  it('scopes recent teams per user id', () => {
    recordRecentTeam('user-1', 'team-a', 'Team A')
    recordRecentTeam('user-2', 'team-b', 'Team B')

    expect(getRecentTeams('user-1')).toEqual([{ id: 'team-a', name: 'Team A' }])
    expect(getRecentTeams('user-2')).toEqual([{ id: 'team-b', name: 'Team B' }])
  })

  it('removes a team so it no longer appears (e.g. after deletion)', () => {
    recordRecentTeam('user-1', 'team-a', 'Team A')
    recordRecentTeam('user-1', 'team-b', 'Team B')

    removeRecentTeam('user-1', 'team-a')

    expect(getRecentTeams('user-1')).toEqual([{ id: 'team-b', name: 'Team B' }])
  })
})
