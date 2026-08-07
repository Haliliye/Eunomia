import { useState } from 'react'
import { useDroppable } from '@dnd-kit/core'
import type { UserStory, UserStoryStatus } from '@/types/userStory'
import type { Label } from '@/types/team'
import StoryCard from './StoryCard'

interface BoardColumnProps {
  status: UserStoryStatus
  title: string
  stories: UserStory[]
  teamName: string
  userNames: Record<string, string>
  labels: Label[]
  wipLimit?: number
  onOpenPanel: (story: UserStory) => void
  onRename?: (name: string) => void
  onDelete?: () => void
  canDelete?: boolean
}

export default function BoardColumn({ status, title, stories, teamName, userNames, labels, wipLimit, onOpenPanel, onRename, onDelete, canDelete }: BoardColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id: status })
  const isOverLimit = wipLimit !== undefined && stories.length > wipLimit
  const [isEditing, setEditing] = useState(false)
  const [name, setName] = useState(title)

  const handleRename = () => {
    setEditing(false)
    if (name.trim() && name.trim() !== title) onRename?.(name.trim())
    else setName(title)
  }

  return (
    <div ref={setNodeRef} className={`board-column ${isOver ? 'is-over' : ''} ${isOverLimit ? 'is-over-wip-limit' : ''}`}>
      <div className="board-column-header">
        {isEditing ? (
          <input
            className="input"
            style={{ fontSize: 13, fontWeight: 600, padding: '2px 6px' }}
            value={name}
            onChange={(e) => setName(e.target.value)}
            onBlur={handleRename}
            onKeyDown={(e) => { if (e.key === 'Enter') handleRename(); if (e.key === 'Escape') { setName(title); setEditing(false) } }}
            autoFocus
          />
        ) : (
          <span
            className="board-column-title"
            style={onRename ? { cursor: 'pointer' } : undefined}
            onClick={() => onRename && setEditing(true)}
            title={onRename ? 'Click to rename' : undefined}
          >
            {title}
          </span>
        )}
        <span
          className="board-column-count mono"
          style={isOverLimit ? { color: 'var(--color-danger)', fontWeight: 700 } : undefined}
          title={isOverLimit ? `Over the WIP limit of ${wipLimit}` : undefined}
        >
          {stories.length}{wipLimit !== undefined && ` / ${wipLimit}`}
        </span>
        {canDelete && onDelete && (
          <button
            className="btn btn-ghost btn-sm"
            style={{ padding: '0 4px', marginLeft: 4 }}
            title="Delete this column"
            onClick={onDelete}
          >
            ✕
          </button>
        )}
      </div>
      {stories.map((story) => (
        <StoryCard key={story.id} story={story} teamName={teamName} userNames={userNames} labels={labels} onOpenPanel={onOpenPanel} />
      ))}
    </div>
  )
}
