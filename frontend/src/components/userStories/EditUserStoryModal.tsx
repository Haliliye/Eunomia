import { useState } from 'react'
import { Link } from 'react-router-dom'
import type { UserStory } from '@/types/userStory'
import type { TeamMember, Label } from '@/types/team'
import { userStoriesApi } from '@/api/userStories'
import CommentSection from '@/components/comments/CommentSection'
import LabelChip from '@/components/common/LabelChip'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'
import { useToast } from '@/context/ToastContext'

interface EditUserStoryModalProps {
  story: UserStory | null
  members: TeamMember[]
  labels: Label[]
  userNames: Record<string, string>
  onClose: () => void
  onSave: (title: string, description: string, dueDate: string | undefined, storyPoints: number | undefined) => void
  onLabelsChanged: () => void
}

export default function EditUserStoryModal({ story, members, labels, userNames, onClose, onSave, onLabelsChanged }: EditUserStoryModalProps) {
  const { showToast } = useToast()
  const [title, setTitle] = useState(story?.title ?? '')
  const [description, setDescription] = useState(story?.description ?? '')
  const [dueDate, setDueDate] = useState(story?.dueDate?.slice(0, 10) ?? '')
  const [storyPoints, setStoryPoints] = useState(story?.storyPoints?.toString() ?? '')
  const [error, setError] = useState<string | null>(null)

  useEscapeToClose(Boolean(story), onClose)
  const containerRef = useFocusTrap(Boolean(story))

  if (!story) return null

  const handleSubmit = () => {
    if (!title.trim()) {
      setError('Title is required.')
      return
    }
    const points = storyPoints.trim() === '' ? undefined : Number(storyPoints)
    if (points !== undefined && (!Number.isInteger(points) || points < 0)) {
      setError('Story points must be a whole number, 0 or more.')
      return
    }

    onSave(title.trim(), description.trim(), dueDate || undefined, points)
  }

  const handleToggleLabel = async (labelId: string) => {
    try {
      if (story.labelIds.includes(labelId)) {
        await userStoriesApi.removeLabel(story.id, labelId)
      } else {
        await userStoriesApi.addLabel(story.id, labelId)
      }
      onLabelsChanged()
    } catch {
      showToast('Could not update labels on this story.', 'error')
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div
        ref={containerRef}
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="edit-story-title"
        onClick={(e) => e.stopPropagation()}
        style={{ maxWidth: 600 }}
      >
        <div className="modal-header" style={{ alignItems: 'center' }}>
          <h2 id="edit-story-title">Edit user story</h2>
          <Link
            to={`/teams/${story.teamId}/stories/${story.id}`}
            onClick={onClose}
            style={{ fontSize: 12.5, color: 'var(--color-brand)', textDecoration: 'none' }}
          >
            Open full story →
          </Link>
        </div>

        <div className="field">
          <label htmlFor="edit-story-title-field">Title</label>
          <input id="edit-story-title-field" className="input" value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} autoFocus />
        </div>
        <div className="field">
          <label htmlFor="edit-story-description">Description (Markdown supported)</label>
          <textarea id="edit-story-description" className="textarea" value={description} onChange={(e) => setDescription(e.target.value)} maxLength={2000} />
        </div>

        <div style={{ display: 'flex', gap: 16 }}>
          <div className="field" style={{ flex: 1 }}>
            <label htmlFor="edit-story-due-date">Due date</label>
            <input id="edit-story-due-date" className="input" type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
          </div>
          <div className="field" style={{ flex: 1 }}>
            <label htmlFor="edit-story-points">Story points</label>
            <input
              id="edit-story-points"
              className="input"
              type="number"
              min={0}
              step={1}
              placeholder="Not estimated"
              value={storyPoints}
              onChange={(e) => setStoryPoints(e.target.value)}
            />
          </div>
        </div>

        {labels.length > 0 && (
          <div className="field">
            <label>Labels</label>
            <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
              {labels.map((label) => {
                const applied = story.labelIds.includes(label.id)
                return (
                  <button
                    key={label.id}
                    onClick={() => handleToggleLabel(label.id)}
                    style={{ border: 'none', background: 'none', padding: 0, cursor: 'pointer', opacity: applied ? 1 : 0.4 }}
                    title={applied ? `Remove ${label.name}` : `Apply ${label.name}`}
                  >
                    <LabelChip label={label} />
                  </button>
                )
              })}
            </div>
          </div>
        )}

        {error && <p className="field-error" role="alert">{error}</p>}

        <div className="modal-footer">
          <button className="btn" onClick={onClose}>Cancel</button>
          <button className="btn btn-primary" onClick={handleSubmit}>Save changes</button>
        </div>

        <div className="card" style={{ marginTop: 20, marginBottom: 0, background: 'var(--color-surface-sunken)' }}>
          <CommentSection userStoryId={story.id} members={members} userNames={userNames} />
        </div>
      </div>
    </div>
  )
}
