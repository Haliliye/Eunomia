import { useEffect, useState } from 'react'
import { usersApi } from '@/api/users'

// Resolves a set of user ids to display names — team members and assignees
// are stored/transmitted as raw ids (from the JWT), so anywhere the UI shows
// "who", it needs this to avoid printing a GUID instead of a name.
export function useUserNames(ids: (string | null | undefined)[]): Record<string, string> {
  const uniqueIds = Array.from(new Set(ids.filter((id): id is string => Boolean(id)))).sort()
  const key = uniqueIds.join(',')
  const [names, setNames] = useState<Record<string, string>>({})

  useEffect(() => {
    if (uniqueIds.length === 0) return
    usersApi.getByIds(uniqueIds).then((users) => {
      setNames((prev) => {
        const next = { ...prev }
        for (const u of users) next[u.id] = u.displayName
        return next
      })
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key])

  return names
}

// Formats "display name, or the raw id if we don't have one yet/at all".
export function displayNameOrId(userNames: Record<string, string>, userId: string): string {
  return userNames[userId] ?? userId
}

// Initials for an avatar — from the display name if we have it, otherwise
// falls back to the first two characters of the raw id.
export function initialsFor(userNames: Record<string, string>, userId: string): string {
  const name = userNames[userId]
  if (!name) return userId.slice(0, 2).toUpperCase()

  const parts = name.trim().split(/\s+/)
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}
