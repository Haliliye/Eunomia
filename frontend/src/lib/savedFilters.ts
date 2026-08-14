import type { UserStoryFilters } from '@/api/userStories'

const MAX_SAVED = 10

export interface SavedFilter {
  id: string
  name: string
  filters: UserStoryFilters
  sprintId?: string
  labelId?: string
}

// Scoped per user id AND team id — a saved filter combo is meaningful only
// within one backlog, and (like recentTeams) shouldn't leak between
// different accounts sharing a browser.
function storageKey(userId: string, teamId: string) {
  return `todoapp:savedFilters:${userId}:${teamId}`
}

export function getSavedFilters(userId: string, teamId: string): SavedFilter[] {
  try {
    const raw = localStorage.getItem(storageKey(userId, teamId))
    return raw ? JSON.parse(raw) : []
  } catch {
    return []
  }
}

export function saveFilter(userId: string, teamId: string, name: string, filters: UserStoryFilters, sprintId?: string, labelId?: string): SavedFilter[] {
  const existing = getSavedFilters(userId, teamId)
  const entry: SavedFilter = { id: crypto.randomUUID(), name, filters, sprintId, labelId }
  const next = [entry, ...existing].slice(0, MAX_SAVED)
  localStorage.setItem(storageKey(userId, teamId), JSON.stringify(next))
  return next
}

export function deleteSavedFilter(userId: string, teamId: string, id: string): SavedFilter[] {
  const next = getSavedFilters(userId, teamId).filter((f) => f.id !== id)
  localStorage.setItem(storageKey(userId, teamId), JSON.stringify(next))
  return next
}
