import { useState } from 'react'
import type { Team } from '@/types/team'
import { teamsApi } from '@/api/teams'
import { useToast } from '@/context/ToastContext'

interface TemplateManagerProps {
  team: Team
  isOwner: boolean
  onChanged: () => void
}

// Owner-managed reusable starting points (bug report, feature request, tech
// debt) — applying one just pre-fills the create-story form and adds a
// checklist, all done client-side (see CreateUserStoryModal).
export default function TemplateManager({ team, isOwner, onChanged }: TemplateManagerProps) {
  const { showToast } = useToast()
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState('')
  const [checklistText, setChecklistText] = useState('')

  if (!isOwner && team.templates.length === 0) return null

  const handleCreate = async () => {
    if (!name.trim()) return
    const checklistItemTexts = checklistText.split('\n').map((l) => l.trim()).filter(Boolean)

    try {
      await teamsApi.createTemplate(team.id, name.trim(), description.trim() || undefined, priority || undefined, checklistItemTexts)
      setName('')
      setDescription('')
      setPriority('')
      setChecklistText('')
      onChanged()
      showToast('Template created.')
    } catch {
      showToast('Could not create that template.', 'error')
    }
  }

  const handleDelete = async (templateId: string) => {
    try {
      await teamsApi.deleteTemplate(team.id, templateId)
      onChanged()
      showToast('Template deleted.')
    } catch {
      showToast('Could not delete that template.', 'error')
    }
  }

  return (
    <div className="card">
      <div className="card-header"><h3>Story templates</h3></div>

      {team.templates.length === 0 ? (
        <p style={{ fontSize: 13 }}>No templates yet.</p>
      ) : (
        <ul style={{ listStyle: 'none', margin: 0, padding: 0, marginBottom: isOwner ? 12 : 0 }}>
          {team.templates.map((t) => (
            <li key={t.id} className="member-row">
              <span>
                <strong>{t.name}</strong>
                {t.checklistItemTexts.length > 0 && (
                  <span style={{ fontSize: 12, color: 'var(--color-ink-muted)' }}> — {t.checklistItemTexts.length} checklist item(s)</span>
                )}
              </span>
              {isOwner && <button className="btn btn-ghost btn-sm" onClick={() => handleDelete(t.id)}>Delete</button>}
            </li>
          ))}
        </ul>
      )}

      {isOwner && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          <input className="input" value={name} onChange={(e) => setName(e.target.value)} placeholder="Template name (e.g. Bug Report)" />
          <textarea className="textarea" value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Default description (optional)" style={{ minHeight: 60 }} />
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <label style={{ fontSize: 13 }}>Default priority</label>
            <select className="pill-select" value={priority} onChange={(e) => setPriority(e.target.value)}>
              <option value="">None</option>
              <option value="Critical">Critical</option>
              <option value="High">High</option>
              <option value="Medium">Medium</option>
              <option value="Low">Low</option>
            </select>
          </div>
          <textarea
            className="textarea"
            value={checklistText}
            onChange={(e) => setChecklistText(e.target.value)}
            placeholder={'Default checklist items, one per line:\nWrite tests\nUpdate docs'}
            style={{ minHeight: 70 }}
          />
          <button className="btn btn-sm" onClick={handleCreate} style={{ alignSelf: 'flex-start' }}>Add template</button>
        </div>
      )}
    </div>
  )
}
