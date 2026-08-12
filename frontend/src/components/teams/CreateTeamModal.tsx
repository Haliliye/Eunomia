import { useState } from 'react'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface CreateTeamModalProps {
  isOpen: boolean
  onClose: () => void
  onCreate: (name: string, description: string) => Promise<void>
}

export default function CreateTeamModal({ isOpen, onClose, onCreate }: CreateTeamModalProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSaving, setSaving] = useState(false)

  useEscapeToClose(isOpen, onClose)
  const containerRef = useFocusTrap(isOpen)

  if (!isOpen) return null

  const handleSubmit = async () => {
    if (!name.trim()) {
      setError('Team name is required.')
      return
    }
    if (name.length > 50) {
      setError('Team name cannot exceed 50 characters.')
      return
    }

    setSaving(true)
    setError(null)
    try {
      await onCreate(name.trim(), description.trim())
      // Only clear once the team is actually created — clearing before this
      // point meant a failed request silently threw away what was typed.
      setName('')
      setDescription('')
    } catch (err: any) {
      setError(err?.response?.data?.error ?? "Couldn't create the team. Please try again.")
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div ref={containerRef} className="modal" role="dialog" aria-modal="true" aria-labelledby="create-team-title" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2 id="create-team-title">New team</h2>
        </div>

        <div className="field">
          <label htmlFor="team-name">Name</label>
          <input id="team-name" className="input" value={name} onChange={(e) => setName(e.target.value)} maxLength={50} autoFocus />
        </div>
        <div className="field">
          <label htmlFor="team-description">Description (optional)</label>
          <textarea id="team-description" className="textarea" value={description} onChange={(e) => setDescription(e.target.value)} />
        </div>
        {error && <p className="field-error" role="alert">{error}</p>}

        <div className="modal-footer">
          <button className="btn" onClick={onClose} disabled={isSaving}>Cancel</button>
          <button className="btn btn-primary" onClick={handleSubmit} disabled={isSaving}>
            {isSaving ? 'Creating…' : 'Create team'}
          </button>
        </div>
      </div>
    </div>
  )
}
