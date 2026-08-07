import { useState } from 'react'
import type { Team } from '@/types/team'
import { teamsApi } from '@/api/teams'
import { useToast } from '@/context/ToastContext'

interface WipLimitsManagerProps {
  team: Team
  isOwner: boolean
  onChanged: () => void
}

// Optional Kanban feature — a column with no limit set behaves exactly as
// before. Owner-only, matches the same permission level as label management.
export default function WipLimitsManager({ team, isOwner, onChanged }: WipLimitsManagerProps) {
  const { showToast } = useToast()
  const [values, setValues] = useState<Record<string, string>>(() =>
    Object.fromEntries(team.columns.map((c) => [c.key, team.wipLimits.find((w) => w.status === c.key)?.limit.toString() ?? '']))
  )

  if (!isOwner && team.wipLimits.length === 0) return null

  const handleSave = async (status: string) => {
    const raw = values[status]?.trim()
    const limit = raw === '' || raw === undefined ? null : Number(raw)

    if (limit !== null && (!Number.isInteger(limit) || limit < 1)) {
      showToast('WIP limit must be a whole number, 1 or more.', 'error')
      return
    }

    try {
      await teamsApi.setWipLimit(team.id, status, limit)
      onChanged()
      showToast(limit === null ? 'WIP limit removed.' : 'WIP limit saved.')
    } catch {
      showToast('Could not save that WIP limit.', 'error')
    }
  }

  return (
    <div className="card">
      <div className="card-header">
        <h3>WIP limits</h3>
      </div>
      <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 12 }}>
        Optional per-column cap on the board — leave blank for no limit. A column over its
        limit is highlighted on the board, but moving a story there is never blocked.
      </p>

      {isOwner ? (
        team.columns.filter((c) => c.key !== 'Done').map((col) => (
          <div key={col.key} style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
            <label style={{ width: 80, fontSize: 13 }}>{col.name}</label>
            <input
              className="input"
              type="number"
              min={1}
              placeholder="No limit"
              value={values[col.key] ?? ''}
              onChange={(e) => setValues((prev) => ({ ...prev, [col.key]: e.target.value }))}
              style={{ maxWidth: 100 }}
            />
            <button className="btn btn-ghost btn-sm" onClick={() => handleSave(col.key)}>Save</button>
          </div>
        ))
      ) : (
        <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
          {team.wipLimits.map((w) => (
            <li key={w.status} style={{ fontSize: 13, padding: '4px 0' }}>{w.status}: {w.limit}</li>
          ))}
        </ul>
      )}
    </div>
  )
}
