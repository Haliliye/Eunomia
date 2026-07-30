import { useState } from 'react'
import type { UserStory } from '@/types/userStory'
import { userStoriesApi } from '@/api/userStories'
import { useToast } from '@/context/ToastContext'
import { displayNameOrId } from '@/hooks/useUserNames'

interface TimeTrackingProps {
  story: UserStory
  userNames: Record<string, string>
  onChange: () => void
}

// US-137 (estimate) + US-138 (log time, running total).
export default function TimeTracking({ story, userNames, onChange }: TimeTrackingProps) {
  const { showToast } = useToast()
  const [estimateInput, setEstimateInput] = useState(story.estimatedHours?.toString() ?? '')
  const [logHours, setLogHours] = useState('')
  const [logNote, setLogNote] = useState('')

  const handleSaveEstimate = async () => {
    const hours = estimateInput.trim() === '' ? null : Number(estimateInput)
    if (hours !== null && (Number.isNaN(hours) || hours < 0)) {
      showToast('Estimate must be a non-negative number.', 'error')
      return
    }
    try {
      await userStoriesApi.setEstimate(story.id, hours)
      onChange()
      showToast('Estimate saved.')
    } catch {
      showToast('Could not save the estimate.', 'error')
    }
  }

  const handleLogTime = async () => {
    const hours = Number(logHours)
    if (!logHours.trim() || Number.isNaN(hours) || hours <= 0) {
      showToast('Enter a positive number of hours.', 'error')
      return
    }
    try {
      await userStoriesApi.logTime(story.id, hours, logNote.trim() || undefined)
      setLogHours('')
      setLogNote('')
      onChange()
      showToast('Time logged.')
    } catch {
      showToast('Could not log time.', 'error')
    }
  }

  return (
    <div className="card">
      <div className="card-header"><h3>Time tracking</h3></div>

      <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 12 }}>
        <label style={{ fontSize: 12.5, color: 'var(--color-ink-muted)' }}>Estimate (hours)</label>
        <input
          className="input"
          type="number"
          min={0}
          step={0.5}
          value={estimateInput}
          onChange={(e) => setEstimateInput(e.target.value)}
          placeholder="Not estimated"
          style={{ maxWidth: 100 }}
        />
        <button className="btn btn-sm" onClick={handleSaveEstimate}>Save</button>
        <span className="mono" style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginLeft: 'auto' }}>
          Logged: {story.totalLoggedHours}h
          {story.estimatedHours !== undefined && ` / ${story.estimatedHours}h estimated`}
        </span>
      </div>

      {story.timeLogEntries.length > 0 && (
        <ul style={{ listStyle: 'none', margin: 0, padding: 0, marginBottom: 12 }}>
          {[...story.timeLogEntries].sort((a, b) => new Date(b.loggedOn).getTime() - new Date(a.loggedOn).getTime()).map((entry) => (
            <li key={entry.id} style={{ fontSize: 13, padding: '6px 0', borderBottom: '1px solid var(--color-border)' }}>
              <strong>{entry.hours}h</strong> by {displayNameOrId(userNames, entry.loggedByUserId)}
              {entry.note && <span> — {entry.note}</span>}
              <div className="mono" style={{ fontSize: 11, color: 'var(--color-ink-faint)' }}>
                {new Date(entry.loggedOn).toLocaleString()}
              </div>
            </li>
          ))}
        </ul>
      )}

      <div style={{ display: 'flex', gap: 8 }}>
        <input className="input" type="number" min={0.25} step={0.25} value={logHours} onChange={(e) => setLogHours(e.target.value)} placeholder="Hours" style={{ maxWidth: 90 }} />
        <input className="input" value={logNote} onChange={(e) => setLogNote(e.target.value)} placeholder="Note (optional)" style={{ flex: 1 }} />
        <button className="btn btn-sm" onClick={handleLogTime}>Log time</button>
      </div>
    </div>
  )
}
