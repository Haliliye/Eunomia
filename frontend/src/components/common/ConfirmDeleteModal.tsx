import { useState } from 'react'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface ConfirmDeleteModalProps {
  isOpen: boolean
  title: string
  confirmationText: string
  description?: string
  onClose: () => void
  onConfirm: () => void
}

// Requires typing the exact name back — a plain "are you sure?" confirm() is
// too easy to click through on reflex for something this destructive (team
// deletion cascades to every one of its user stories).
export default function ConfirmDeleteModal({ isOpen, title, confirmationText, description, onClose, onConfirm }: ConfirmDeleteModalProps) {
  const [typed, setTyped] = useState('')
  useEscapeToClose(isOpen, onClose)
  const containerRef = useFocusTrap(isOpen)

  if (!isOpen) return null

  const isMatch = typed === confirmationText

  const handleConfirm = () => {
    if (!isMatch) return
    onConfirm()
    setTyped('')
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div ref={containerRef} className="modal" role="dialog" aria-modal="true" aria-labelledby="confirm-delete-title" onClick={(e) => e.stopPropagation()}>
        <h2 id="confirm-delete-title">{title}</h2>
        {description && <p style={{ marginBottom: 8 }}>{description}</p>}
        <p style={{ fontSize: 13, marginBottom: 8 }}>
          To confirm, type <strong className="mono">{confirmationText}</strong> below:
        </p>
        <input
          className="input"
          value={typed}
          onChange={(e) => setTyped(e.target.value)}
          autoFocus
          onKeyDown={(e) => { if (e.key === 'Enter' && isMatch) handleConfirm() }}
        />
        <div className="modal-actions" style={{ marginTop: 16 }}>
          <button className="btn" onClick={onClose}>Cancel</button>
          <button className="btn btn-danger" disabled={!isMatch} onClick={handleConfirm}>Delete</button>
        </div>
      </div>
    </div>
  )
}
