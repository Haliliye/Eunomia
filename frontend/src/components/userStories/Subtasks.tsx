import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { UserStory } from '@/types/userStory'
import { userStoriesApi } from '@/api/userStories'
import { useToast } from '@/context/ToastContext'
import StatusBadge from '@/components/common/StatusBadge'
import { ticketCode } from '@/lib/ticketCode'

interface SubtasksProps {
  story: UserStory
  teamId: string
  teamName: string
}

// Matches Jira's own model — a subtask can't have its own subtasks (enforced
// server-side too, see CreateSubtaskCommandHandler), so this section simply
// doesn't render on a story that's itself a subtask.
export default function Subtasks({ story, teamId, teamName }: SubtasksProps) {
  const { showToast } = useToast()
  const [subtasks, setSubtasks] = useState<UserStory[]>([])
  const [isLoading, setLoading] = useState(true)
  const [newTitle, setNewTitle] = useState('')

  const load = () => {
    setLoading(true)
    userStoriesApi.getSubtasks(story.id).then(setSubtasks).finally(() => setLoading(false))
  }

  useEffect(load, [story.id])

  if (story.parentId) return null

  const handleAdd = async () => {
    if (!newTitle.trim()) return
    try {
      await userStoriesApi.createSubtask(story.id, newTitle.trim())
      setNewTitle('')
      load()
    } catch {
      showToast('Could not add that subtask.', 'error')
    }
  }

  return (
    <div className="card">
      <div className="card-header"><h3>Subtasks {subtasks.length > 0 && `(${subtasks.length})`}</h3></div>

      {isLoading ? (
        <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)' }}>Loading…</p>
      ) : (
        subtasks.map((s) => (
          <Link key={s.id} to={`/teams/${teamId}/stories/${s.id}`} className="story-subtask-row">
            <span className="mono" style={{ fontSize: 11.5, color: 'var(--color-ink-faint)' }}>{ticketCode(teamName, s.id)}</span>
            <span style={{ flex: 1 }}>{s.title}</span>
            <StatusBadge status={s.status} />
          </Link>
        ))
      )}

      <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
        <input
          className="input"
          placeholder="Add a subtask…"
          value={newTitle}
          onChange={(e) => setNewTitle(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleAdd()}
        />
        <button className="btn" onClick={handleAdd} disabled={!newTitle.trim()}>Add</button>
      </div>
    </div>
  )
}
