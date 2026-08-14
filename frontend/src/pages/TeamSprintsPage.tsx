import { useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { sprintsApi, type SprintCompletionSummary } from '@/api/sprints'
import type { Sprint } from '@/types/sprint'
import { useToast } from '@/context/ToastContext'
import { useConfirm } from '@/context/ConfirmContext'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'
import type { TeamOutletContext } from './TeamShellPage'

const STATUS_CLASS: Record<Sprint['status'], string> = {
  Planned: 'badge badge-status-todo',
  Active: 'badge badge-status-inprogress',
  Completed: 'badge badge-status-done',
}

function defaultDates() {
  const start = new Date()
  const end = new Date()
  end.setDate(end.getDate() + 14) // a two-week sprint by default — the most common length
  return { start: start.toISOString().slice(0, 10), end: end.toISOString().slice(0, 10) }
}

export default function TeamSprintsPage() {
  const { team } = useOutletContext<TeamOutletContext>()
  const { showToast } = useToast()
  const confirm = useConfirm()
  const [completionSummary, setCompletionSummary] = useState<SprintCompletionSummary | null>(null)
  useEscapeToClose(completionSummary !== null, () => setCompletionSummary(null))
  const completionModalRef = useFocusTrap(completionSummary !== null)
  const [sprints, setSprints] = useState<Sprint[]>([])
  const [isLoading, setLoading] = useState(true)
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [name, setName] = useState('')
  const [dates, setDates] = useState(defaultDates)
  const [error, setError] = useState<string | null>(null)

  const load = () => {
    sprintsApi.getForTeam(team.id).then(setSprints).finally(() => setLoading(false))
  }

  useEffect(load, [team.id])

  const handleCreate = async () => {
    if (!name.trim()) {
      setError('Sprint name is required.')
      return
    }
    try {
      await sprintsApi.create(team.id, name.trim(), dates.start, dates.end)
      setName('')
      setDates(defaultDates())
      setCreateOpen(false)
      setError(null)
      load()
      showToast('Sprint created.')
    } catch {
      setError('Could not create the sprint.')
    }
  }

  const handleStart = async (sprint: Sprint) => {
    try {
      await sprintsApi.start(sprint.id)
      load()
      showToast(`"${sprint.name}" is now active.`)
    } catch (err) {
      showToast(extractErrorMessage(err), 'error')
    }
  }

  const handleComplete = async (sprint: Sprint) => {
    const confirmed = await confirm({
      title: `Complete "${sprint.name}"?`,
      description: 'Any stories not marked Done will move back to the backlog.',
      confirmLabel: 'Complete',
    })
    if (!confirmed) return

    try {
      const summary = await sprintsApi.complete(sprint.id)
      load()
      setCompletionSummary(summary)
    } catch (err) {
      showToast(extractErrorMessage(err), 'error')
    }
  }

  if (isLoading) return <p>Loading…</p>

  return (
    <div>
      <div className="card-header">
        <h2>Sprints</h2>
        <button className="btn btn-primary btn-sm" onClick={() => setCreateOpen((v) => !v)}>+ New sprint</button>
      </div>

      {isCreateOpen && (
        <div className="card">
          <div className="field">
            <label>Name</label>
            <input className="input" value={name} onChange={(e) => setName(e.target.value)} placeholder="Sprint 1" autoFocus />
          </div>
          <div style={{ display: 'flex', gap: 12 }}>
            <div className="field" style={{ flex: 1 }}>
              <label>Start date</label>
              <input className="input" type="date" value={dates.start} onChange={(e) => setDates((d) => ({ ...d, start: e.target.value }))} />
            </div>
            <div className="field" style={{ flex: 1 }}>
              <label>End date</label>
              <input className="input" type="date" value={dates.end} onChange={(e) => setDates((d) => ({ ...d, end: e.target.value }))} />
            </div>
          </div>
          {error && <p className="field-error" role="alert">{error}</p>}
          <button className="btn btn-primary" onClick={handleCreate}>Create sprint</button>
        </div>
      )}

      {sprints.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-title">No sprints yet</div>
          <p>Create one to start planning work into time-boxed iterations.</p>
        </div>
      ) : (
        <div className="backlog-list">
          {sprints.map((sprint) => (
            <div className="backlog-row" key={sprint.id} style={{ flexWrap: 'wrap' }}>
              <span className={STATUS_CLASS[sprint.status]}>{sprint.status}</span>
              <span className="backlog-title">{sprint.name}</span>
              <span className="mono" style={{ fontSize: 12, color: 'var(--color-ink-faint)' }}>
                {new Date(sprint.startDate).toLocaleDateString()} – {new Date(sprint.endDate).toLocaleDateString()}
              </span>
              <div className="row-actions">
                {sprint.status === 'Planned' && (
                  <button className="btn btn-sm" onClick={() => handleStart(sprint)}>Start</button>
                )}
                {sprint.status === 'Active' && (
                  <button className="btn btn-sm" onClick={() => handleComplete(sprint)}>Complete</button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {completionSummary && (
        <div className="modal-overlay" onClick={() => setCompletionSummary(null)}>
          <div ref={completionModalRef} className="modal" role="dialog" aria-modal="true" style={{ maxWidth: 480 }} onClick={(e) => e.stopPropagation()}>
            <h2>"{completionSummary.sprintName}" completed</h2>

            <div style={{ display: 'flex', gap: 12, margin: '16px 0' }}>
              <div className="card" style={{ flex: 1, textAlign: 'center', padding: 12 }}>
                <div style={{ fontSize: 24, fontWeight: 700, color: 'var(--color-done)' }}>{completionSummary.completedCount}</div>
                <div style={{ fontSize: 12, color: 'var(--color-ink-muted)' }}>completed ({completionSummary.completedPoints} pts)</div>
              </div>
              <div className="card" style={{ flex: 1, textAlign: 'center', padding: 12 }}>
                <div style={{ fontSize: 24, fontWeight: 700, color: 'var(--color-ink-muted)' }}>{completionSummary.carriedOverCount}</div>
                <div style={{ fontSize: 12, color: 'var(--color-ink-muted)' }}>carried to backlog ({completionSummary.carriedOverPoints} pts)</div>
              </div>
            </div>

            {completionSummary.carriedOverStories.length > 0 && (
              <>
                <p style={{ fontSize: 12.5, fontWeight: 600, marginBottom: 6 }}>Moved back to the backlog:</p>
                <ul style={{ fontSize: 13, paddingLeft: 18, margin: 0, maxHeight: 200, overflowY: 'auto' }}>
                  {completionSummary.carriedOverStories.map((s) => (
                    <li key={s.id}>{s.title} <span className="mono" style={{ color: 'var(--color-ink-faint)' }}>({s.status})</span></li>
                  ))}
                </ul>
              </>
            )}

            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn btn-primary" onClick={() => setCompletionSummary(null)}>Done</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function extractErrorMessage(err: unknown): string {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const response = (err as { response?: { data?: { error?: string } } }).response
    if (response?.data?.error) return response.data.error
  }
  return 'Something went wrong.'
}
