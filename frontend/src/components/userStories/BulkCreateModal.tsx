import { useState } from 'react'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface BulkCreateModalProps {
  isOpen: boolean
  onClose: () => void
  onCreate: (titles: string[]) => Promise<void>
}

// Trello/Linear-style quick add — paste or type a list, one story per line.
export default function BulkCreateModal({ isOpen, onClose, onCreate }: BulkCreateModalProps) {
  const [text, setText] = useState('')
  const [isSaving, setSaving] = useState(false)

  useEscapeToClose(isOpen, onClose)
  const containerRef = useFocusTrap(isOpen)

  if (!isOpen) return null

  const titles = text.split('\n').map((l) => l.trim()).filter(Boolean)

  const handleSubmit = async () => {
    if (titles.length === 0) return
    setSaving(true)
    try {
      await onCreate(titles)
      // Only clear once the stories are actually created — clearing before
      // this point meant a failed request silently threw away what was typed.
      setText('')
    } catch {
      // The parent already surfaces a toast on failure — this just needs to
      // stop here so the catch above (clearing the text) doesn't run.
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div ref={containerRef} className="modal" role="dialog" aria-modal="true" aria-labelledby="bulk-create-title" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2 id="bulk-create-title">Add multiple stories</h2>
        </div>

        <div className="field">
          <label htmlFor="bulk-create-textarea">One title per line</label>
          <textarea
            id="bulk-create-textarea"
            className="textarea"
            value={text}
            onChange={(e) => setText(e.target.value)}
            placeholder={'Fix login redirect bug\nAdd dark mode toggle\nWrite onboarding docs'}
            style={{ minHeight: 160 }}
            autoFocus
          />
        </div>

        <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
          {titles.length} {titles.length === 1 ? 'story' : 'stories'} will be created. Blank lines are skipped.
        </p>

        <div className="modal-footer">
          <button className="btn" onClick={onClose} disabled={isSaving}>Cancel</button>
          <button className="btn btn-primary" disabled={titles.length === 0 || isSaving} onClick={handleSubmit}>
            {isSaving ? 'Creating…' : `Create ${titles.length || ''} ${titles.length === 1 ? 'story' : 'stories'}`}
          </button>
        </div>
      </div>
    </div>
  )
}
