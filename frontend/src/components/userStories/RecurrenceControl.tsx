import { useState } from 'react'
import type { UserStory } from '@/types/userStory'
import { userStoriesApi } from '@/api/userStories'
import { useToast } from '@/context/ToastContext'

type Frequency = 'Daily' | 'Weekly' | 'Monthly'

interface RecurrenceControlProps {
  story: UserStory
  onChange: () => void
}

// US-128/130: set/edit/cancel a recurrence pattern. US-129 (auto-generating the
// next occurrence on completion) is entirely backend-driven — this component
// only manages the pattern itself.
export default function RecurrenceControl({ story, onChange }: RecurrenceControlProps) {
  const { showToast } = useToast()
  const [frequency, setFrequency] = useState<Frequency | ''>(story.recurrenceFrequency ?? '')
  const [endDate, setEndDate] = useState(story.recurrenceEndDate?.slice(0, 10) ?? '')

  const handleSave = async () => {
    try {
      await userStoriesApi.setRecurrence(story.id, frequency || null, frequency ? (endDate || null) : null)
      onChange()
      showToast(frequency ? 'Recurrence saved.' : 'Recurrence turned off.')
    } catch {
      showToast('Could not update recurrence.', 'error')
    }
  }

  return (
    <div className="card">
      <div className="card-header"><h3>🔁 Recurrence</h3></div>
      <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 8 }}>
        Automatically creates a new occurrence with the next due date when this story is marked Done.
      </p>
      <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
        <select className="pill-select" value={frequency} onChange={(e) => setFrequency(e.target.value as Frequency | '')}>
          <option value="">Does not repeat</option>
          <option value="Daily">Daily</option>
          <option value="Weekly">Weekly</option>
          <option value="Monthly">Monthly</option>
        </select>
        {frequency && (
          <>
            <label style={{ fontSize: 12.5, color: 'var(--color-ink-muted)' }}>Ends on (optional)</label>
            <input className="input" type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} style={{ maxWidth: 160 }} />
          </>
        )}
        <button className="btn btn-sm" onClick={handleSave}>Save</button>
      </div>
    </div>
  )
}
