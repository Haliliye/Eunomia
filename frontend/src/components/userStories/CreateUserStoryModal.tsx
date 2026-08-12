import { useState } from 'react'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'
import type { StoryTemplate } from '@/api/teams'

interface CreateUserStoryModalProps {
  isOpen: boolean
  templates?: StoryTemplate[]
  onClose: () => void
  onCreate: (title: string, description: string, priority?: string, checklistItemTexts?: string[]) => Promise<void>
}

export default function CreateUserStoryModal({ isOpen, templates, onClose, onCreate }: CreateUserStoryModalProps) {
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [templateId, setTemplateId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSaving, setSaving] = useState(false)

  useEscapeToClose(isOpen, onClose)
  const containerRef = useFocusTrap(isOpen)

  if (!isOpen) return null

  const handleTemplateChange = (id: string) => {
    setTemplateId(id)
    const template = templates?.find((t) => t.id === id)
    if (template?.defaultDescription && !description.trim()) {
      setDescription(template.defaultDescription)
    }
  }

  const handleSubmit = async () => {
    if (!title.trim()) {
      setError('Title is required.')
      return
    }
    if (title.length > 200) {
      setError('Title cannot exceed 200 characters.')
      return
    }
    if (description.length > 2000) {
      setError('Description cannot exceed 2000 characters.')
      return
    }

    const template = templates?.find((t) => t.id === templateId)
    setSaving(true)
    setError(null)
    try {
      await onCreate(title.trim(), description.trim(), template?.defaultPriority, template?.checklistItemTexts)
      // Only clear once the story is actually created — clearing before this
      // point meant a failed request silently threw away what was typed.
      setTitle('')
      setDescription('')
      setTemplateId('')
    } catch (err: any) {
      setError(err?.response?.data?.error ?? "Couldn't create the story. Please try again.")
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div ref={containerRef} className="modal" role="dialog" aria-modal="true" aria-labelledby="create-story-title" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2 id="create-story-title">New user story</h2>
        </div>

        {templates && templates.length > 0 && (
          <div className="field">
            <label htmlFor="story-template">Use template (optional)</label>
            <select id="story-template" className="pill-select" value={templateId} onChange={(e) => handleTemplateChange(e.target.value)} style={{ width: '100%' }}>
              <option value="">— No template —</option>
              {templates.map((t) => (
                <option key={t.id} value={t.id}>{t.name}</option>
              ))}
            </select>
          </div>
        )}

        <div className="field">
          <label htmlFor="story-title">Title</label>
          <input id="story-title" className="input" value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} autoFocus />
        </div>
        <div className="field">
          <label htmlFor="story-description">Description (optional, Markdown supported)</label>
          <textarea id="story-description" className="textarea" value={description} onChange={(e) => setDescription(e.target.value)} maxLength={2000} />
        </div>
        {error && <p className="field-error" role="alert">{error}</p>}

        <div className="modal-footer">
          <button className="btn" onClick={onClose} disabled={isSaving}>Cancel</button>
          <button className="btn btn-primary" onClick={handleSubmit} disabled={isSaving}>
            {isSaving ? 'Creating…' : 'Create story'}
          </button>
        </div>
      </div>
    </div>
  )
}
