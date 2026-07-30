import { useState } from 'react'
import type { ChecklistItem } from '@/types/userStory'
import { userStoriesApi } from '@/api/userStories'
import { useToast } from '@/context/ToastContext'

interface ChecklistProps {
  userStoryId: string
  items: ChecklistItem[]
  onChange: () => void
}

export default function Checklist({ userStoryId, items, onChange }: ChecklistProps) {
  const { showToast } = useToast()
  const [newItemText, setNewItemText] = useState('')
  const sorted = [...items].sort((a, b) => a.order - b.order)
  const completedCount = sorted.filter((i) => i.isCompleted).length

  const handleAdd = async () => {
    if (!newItemText.trim()) return
    try {
      await userStoriesApi.addChecklistItem(userStoryId, newItemText.trim())
      setNewItemText('')
      onChange()
    } catch {
      showToast('Could not add that checklist item.', 'error')
    }
  }

  const handleToggle = async (item: ChecklistItem) => {
    try {
      await userStoriesApi.toggleChecklistItem(userStoryId, item.id)
      onChange()
    } catch {
      showToast('Could not update that item.', 'error')
    }
  }

  const handleRemove = async (item: ChecklistItem) => {
    try {
      await userStoriesApi.removeChecklistItem(userStoryId, item.id)
      onChange()
    } catch {
      showToast('Could not remove that item.', 'error')
    }
  }

  const handleMove = async (index: number, direction: -1 | 1) => {
    const targetIndex = index + direction
    if (targetIndex < 0 || targetIndex >= sorted.length) return

    const reordered = [...sorted]
    ;[reordered[index], reordered[targetIndex]] = [reordered[targetIndex], reordered[index]]

    try {
      await userStoriesApi.reorderChecklistItems(userStoryId, reordered.map((i) => i.id))
      onChange()
    } catch {
      showToast('Could not reorder the checklist.', 'error')
    }
  }

  return (
    <div>
      <div className="card-header">
        <h3>Checklist</h3>
        {sorted.length > 0 && (
          <span className="mono" style={{ fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
            {completedCount}/{sorted.length}
          </span>
        )}
      </div>

      {sorted.length > 0 && (
        <div className="dashboard-track" style={{ marginBottom: 12 }}>
          <div
            className="dashboard-fill"
            style={{ width: `${(completedCount / sorted.length) * 100}%`, background: 'var(--color-brand)' }}
          />
        </div>
      )}

      <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
        {sorted.map((item, index) => (
          <li key={item.id} style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '6px 0' }}>
            <input type="checkbox" checked={item.isCompleted} onChange={() => handleToggle(item)} />
            <span style={{ flex: 1, textDecoration: item.isCompleted ? 'line-through' : undefined, color: item.isCompleted ? 'var(--color-ink-faint)' : undefined }}>
              {item.text}
            </span>
            <button className="btn btn-ghost btn-sm" disabled={index === 0} onClick={() => handleMove(index, -1)} aria-label="Move up">↑</button>
            <button className="btn btn-ghost btn-sm" disabled={index === sorted.length - 1} onClick={() => handleMove(index, 1)} aria-label="Move down">↓</button>
            <button className="btn btn-ghost btn-sm" onClick={() => handleRemove(item)} aria-label="Remove item">✕</button>
          </li>
        ))}
      </ul>

      <div style={{ display: 'flex', gap: 8, marginTop: 8 }}>
        <input
          className="input"
          value={newItemText}
          onChange={(e) => setNewItemText(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter') handleAdd() }}
          placeholder="Add a checklist item"
          style={{ flex: 1 }}
        />
        <button className="btn btn-sm" onClick={handleAdd}>Add</button>
      </div>
    </div>
  )
}
