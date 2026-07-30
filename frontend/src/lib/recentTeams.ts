const MAX_RECENT = 5

export interface RecentTeam {
  id: string
  name: string
}

// Scoped per user id — without this, switching accounts on the same browser
// (e.g. testing with several seeded accounts) leaked one account's recently
// visited teams into another's sidebar, since localStorage is shared across
// whoever is logged in on that browser.
function storageKey(userId: string) {
  return `todoapp:recentTeams:${userId}`
}

export function getRecentTeams(userId: string): RecentTeam[] {
  try {
    const raw = localStorage.getItem(storageKey(userId))
    return raw ? JSON.parse(raw) : []
  } catch {
    return []
  }
}

// Called whenever a team page loads — moves that team to the front,
// dedupes, and caps the list so "Recent" stays short and actually recent.
export function recordRecentTeam(userId: string, teamId: string, teamName: string) {
  const existing = getRecentTeams(userId).filter((t) => t.id !== teamId)
  const next = [{ id: teamId, name: teamName }, ...existing].slice(0, MAX_RECENT)
  localStorage.setItem(storageKey(userId), JSON.stringify(next))
}
