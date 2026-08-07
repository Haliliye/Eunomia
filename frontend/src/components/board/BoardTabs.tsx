import { useState } from 'react'
import { boardsApi, type Board } from '@/api/boards'
import type { Sprint } from '@/types/sprint'
import { useToast } from '@/context/ToastContext'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface BoardTabsProps {
  teamId: string
  boards: Board[]
  sprints: Sprint[]
  selectedBoardId: string | null
  onSelect: (boardId: string | null) => void
  onChanged: () => void
}

export default function BoardTabs({ teamId, boards, sprints, selectedBoardId, onSelect, onChanged }: BoardTabsProps) {
  const [editingBoard, setEditingBoard] = useState<Board | 'new' | null>(null)

  const selectedBoard = boards.find((b) => b.id === selectedBoardId) ?? null

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12, flexWrap: 'wrap' }}>
      <nav className="team-tabs" style={{ marginBottom: 0, flex: 1 }}>
        <button
          className={`team-tab ${selectedBoardId === null ? 'active' : ''}`}
          style={{ background: 'none', border: 'none', borderBottom: '2px solid transparent', cursor: 'pointer' }}
          onClick={() => onSelect(null)}
        >
          All
        </button>
        {boards.map((b) => (
          <button
            key={b.id}
            className={`team-tab ${selectedBoardId === b.id ? 'active' : ''}`}
            style={{ background: 'none', border: 'none', borderBottom: '2px solid transparent', cursor: 'pointer' }}
            onClick={() => onSelect(b.id)}
          >
            {b.name}
          </button>
        ))}
      </nav>

      <button className="btn btn-ghost btn-sm" onClick={() => setEditingBoard('new')}>+ New board</button>
      {selectedBoard && (
        <button className="btn btn-ghost btn-sm" onClick={() => setEditingBoard(selectedBoard)}>Edit board</button>
      )}

      {editingBoard && (
        <BoardEditModal
          teamId={teamId}
          board={editingBoard === 'new' ? null : editingBoard}
          sprints={sprints}
          onClose={() => setEditingBoard(null)}
          onSaved={(board, deleted) => {
            setEditingBoard(null)
            onChanged()
            if (deleted) onSelect(null)
            else onSelect(board.id)
          }}
        />
      )}
    </div>
  )
}

interface BoardEditModalProps {
  teamId: string
  board: Board | null
  sprints: Sprint[]
  onClose: () => void
  onSaved: (board: Board, deleted?: boolean) => void
}

function BoardEditModal({ teamId, board, sprints, onClose, onSaved }: BoardEditModalProps) {
  const { showToast } = useToast()
  const [name, setName] = useState(board?.name ?? '')
  const [sprintId, setSprintId] = useState(board?.sprintId ?? '')
  const [isSaving, setSaving] = useState(false)
  useEscapeToClose(true, onClose)
  const containerRef = useFocusTrap(true)

  const handleSave = async () => {
    if (!name.trim()) return
    setSaving(true)
    try {
      if (board) {
        await boardsApi.rename(board.id, name.trim(), sprintId || undefined)
        onSaved({ ...board, name: name.trim(), sprintId: sprintId || undefined })
      } else {
        const created = await boardsApi.create(teamId, name.trim(), sprintId || undefined)
        onSaved(created)
      }
    } catch {
      showToast("Couldn't save this board.", 'error')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!board) return
    if (!window.confirm(`Delete the "${board.name}" board? This only removes the saved view — its stories aren't affected.`)) return
    setSaving(true)
    try {
      await boardsApi.delete(board.id)
      onSaved(board, true)
    } catch {
      showToast("Couldn't delete this board.", 'error')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" ref={containerRef} role="dialog" aria-modal="true" style={{ maxWidth: 420 }} onClick={(e) => e.stopPropagation()}>
        <h2>{board ? 'Edit board' : 'New board'}</h2>

        <div className="field">
          <label htmlFor="board-name">Name</label>
          <input id="board-name" className="input" value={name} onChange={(e) => setName(e.target.value)} maxLength={50} autoFocus />
        </div>

        <div className="field">
          <label htmlFor="board-sprint">Sprint (optional)</label>
          <select id="board-sprint" className="select" value={sprintId} onChange={(e) => setSprintId(e.target.value)}>
            <option value="">Whole backlog (all sprints)</option>
            {sprints.map((s) => (
              <option key={s.id} value={s.id}>{s.name} ({s.status})</option>
            ))}
          </select>
        </div>
        <p style={{ fontSize: 11.5, color: 'var(--color-ink-faint)', marginTop: 4 }}>
          A board is a saved, shareable view of this team's board — the columns and stories are the
          same for everyone, only the sprint scope is remembered per board.
        </p>

        <div className="modal-actions" style={{ marginTop: 16, justifyContent: board ? 'space-between' : 'flex-end' }}>
          {board && <button className="btn btn-ghost" style={{ color: 'var(--color-danger)' }} onClick={handleDelete} disabled={isSaving}>Delete</button>}
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn" onClick={onClose}>Cancel</button>
            <button className="btn btn-primary" onClick={handleSave} disabled={isSaving || !name.trim()}>Save</button>
          </div>
        </div>
      </div>
    </div>
  )
}
