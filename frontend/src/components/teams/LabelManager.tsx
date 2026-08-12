import { useState } from 'react'
import type { Team, Label } from '@/types/team'
import { teamsApi } from '@/api/teams'
import { useToast } from '@/context/ToastContext'
import { useConfirm } from '@/context/ConfirmContext'
import LabelChip from '@/components/common/LabelChip'

const DEFAULT_COLORS = ['#0B6E63', '#7C4DBD', '#2B6CB5', '#B48A0A', '#C23B6B', '#B3261E', '#2F6F4E']

interface LabelManagerProps {
  team: Team
  isOwner: boolean
  onChanged: () => void
}

export default function LabelManager({ team, isOwner, onChanged }: LabelManagerProps) {
  const { showToast } = useToast()
  const confirm = useConfirm()
  const [name, setName] = useState('')
  const [color, setColor] = useState(DEFAULT_COLORS[0])

  const handleCreate = async () => {
    if (!name.trim()) return
    try {
      await teamsApi.createLabel(team.id, name.trim(), color)
      setName('')
      onChanged()
      showToast('Label created.')
    } catch {
      showToast('Could not create that label — the name may already be in use.', 'error')
    }
  }

  const handleDelete = async (label: Label) => {
    const confirmed = await confirm({
      title: `Delete the "${label.name}" label?`,
      description: 'It will be removed from every story that has it.',
      confirmLabel: 'Delete',
      danger: true,
    })
    if (!confirmed) return

    try {
      await teamsApi.deleteLabel(team.id, label.id)
      onChanged()
      showToast('Label deleted.')
    } catch {
      showToast('Could not delete that label.', 'error')
    }
  }

  if (!isOwner && team.labels.length === 0) return null

  return (
    <div className="card">
      <div className="card-header">
        <h3>Labels</h3>
      </div>

      <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', marginBottom: isOwner ? 12 : 0 }}>
        {team.labels.length === 0 ? (
          <p style={{ fontSize: 13 }}>No labels yet.</p>
        ) : (
          team.labels.map((label) => (
            <LabelChip key={label.id} label={label} onRemove={isOwner ? () => handleDelete(label) : undefined} />
          ))
        )}
      </div>

      {isOwner && (
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <input
            className="input"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="New label name"
            style={{ flex: 1 }}
          />
          <div style={{ display: 'flex', gap: 4 }}>
            {DEFAULT_COLORS.map((c) => (
              <button
                key={c}
                onClick={() => setColor(c)}
                aria-label={`Choose color ${c}`}
                style={{
                  width: 20, height: 20, borderRadius: '50%', background: c, cursor: 'pointer',
                  border: color === c ? '2px solid var(--color-ink)' : '1px solid transparent',
                }}
              />
            ))}
          </div>
          <button className="btn btn-sm" onClick={handleCreate}>Add</button>
        </div>
      )}
    </div>
  )
}
