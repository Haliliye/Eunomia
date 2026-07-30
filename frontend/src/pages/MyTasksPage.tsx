import { useEffect, useState } from 'react'
import { personalTasksApi } from '@/api/personalTasks'
import { teamsApi } from '@/api/teams'
import type { PersonalTask } from '@/types/personalTask'
import type { Team } from '@/types/team'
import { useToast } from '@/context/ToastContext'
import { SkeletonTable } from '@/components/common/Skeleton'

// US-140/141: a private to-do list outside any team — never shown on a team
// board, only ever visible to its owner.
export default function MyTasksPage() {
  const { showToast } = useToast()
  const [tasks, setTasks] = useState<PersonalTask[]>([])
  const [myTeams, setMyTeams] = useState<Team[]>([])
  const [isLoading, setLoading] = useState(true)
  const [title, setTitle] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [convertingTaskId, setConvertingTaskId] = useState<string | null>(null)
  const [convertTeamId, setConvertTeamId] = useState('')

  const load = () => {
    personalTasksApi.getMine().then(setTasks).finally(() => setLoading(false))
  }

  useEffect(() => {
    load()
    teamsApi.getMyTeams(1, 50).then((result) => setMyTeams(result.items))
  }, [])

  const handleCreate = async () => {
    if (!title.trim()) return
    try {
      await personalTasksApi.create(title.trim(), undefined, dueDate || undefined)
      setTitle('')
      setDueDate('')
      load()
    } catch {
      showToast('Could not create that task.', 'error')
    }
  }

  const handleToggle = async (task: PersonalTask) => {
    try {
      await personalTasksApi.toggle(task.id, !task.isCompleted)
      load()
    } catch {
      showToast('Could not update that task.', 'error')
    }
  }

  const handleDelete = async (task: PersonalTask) => {
    const confirmed = window.confirm(`Delete "${task.title}"?`)
    if (!confirmed) return
    try {
      await personalTasksApi.delete(task.id)
      load()
    } catch {
      showToast('Could not delete that task.', 'error')
    }
  }

  const handleConvert = async () => {
    if (!convertingTaskId || !convertTeamId) return
    try {
      await personalTasksApi.convert(convertingTaskId, convertTeamId)
      setConvertingTaskId(null)
      setConvertTeamId('')
      load()
      showToast('Converted to a team user story.')
    } catch {
      showToast('Could not convert that task.', 'error')
    }
  }

  const visibleTasks = tasks.filter((t) => !t.convertedToUserStoryId)

  return (
    <section>
      <div className="page-header">
        <div>
          <span className="page-header-eyebrow">Personal</span>
          <h1>My Tasks</h1>
          <p>Private to-dos — never shown on any team board.</p>
        </div>
      </div>

      <div className="card">
        <div style={{ display: 'flex', gap: 8 }}>
          <input className="input" value={title} onChange={(e) => setTitle(e.target.value)} placeholder="New task" style={{ flex: 1 }} onKeyDown={(e) => { if (e.key === 'Enter') handleCreate() }} />
          <input className="input" type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} style={{ maxWidth: 160 }} />
          <button className="btn btn-primary btn-sm" onClick={handleCreate}>Add</button>
        </div>
      </div>

      {isLoading ? (
        <SkeletonTable />
      ) : visibleTasks.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-title">No personal tasks yet</div>
          <p>Add one above to start tracking your own to-dos.</p>
        </div>
      ) : (
        <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
          {visibleTasks.map((task) => (
            <li key={task.id} className="member-row">
              <span style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <input type="checkbox" checked={task.isCompleted} onChange={() => handleToggle(task)} />
                <span style={{ textDecoration: task.isCompleted ? 'line-through' : undefined, color: task.isCompleted ? 'var(--color-ink-faint)' : undefined }}>
                  {task.title}
                </span>
                {task.dueDate && <span className="mono" style={{ fontSize: 11, color: 'var(--color-ink-faint)' }}>{new Date(task.dueDate).toLocaleDateString()}</span>}
              </span>
              <span style={{ display: 'flex', gap: 6 }}>
                <button className="btn btn-ghost btn-sm" onClick={() => setConvertingTaskId(task.id)}>Convert to team item</button>
                <button className="btn btn-ghost btn-sm" onClick={() => handleDelete(task)}>Delete</button>
              </span>
            </li>
          ))}
        </ul>
      )}

      {convertingTaskId && (
        <div className="modal-overlay" onClick={() => setConvertingTaskId(null)}>
          <div className="modal" role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
            <h2>Convert to a team user story</h2>
            <p style={{ fontSize: 13, marginBottom: 8 }}>Only teams you're a member of are shown.</p>
            <select className="pill-select" value={convertTeamId} onChange={(e) => setConvertTeamId(e.target.value)} style={{ width: '100%' }}>
              <option value="">Choose a team…</option>
              {myTeams.map((t) => (
                <option key={t.id} value={t.id}>{t.name}</option>
              ))}
            </select>
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={() => setConvertingTaskId(null)}>Cancel</button>
              <button className="btn btn-primary" disabled={!convertTeamId} onClick={handleConvert}>Convert</button>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}
