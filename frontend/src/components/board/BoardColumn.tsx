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
}

export default function BoardColumn({ status, title, stories, teamName, userNames, labels, wipLimit, onOpenPanel }: BoardColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id: status })
  const isOverLimit = wipLimit !== undefined && stories.length > wipLimit

  return (
    <div ref={setNodeRef} className={`board-column ${isOver ? 'is-over' : ''} ${isOverLimit ? 'is-over-wip-limit' : ''}`}>
      <div className="board-column-header">
        <span className="board-column-title">{title}</span>
        <span
          className="board-column-count mono"
          style={isOverLimit ? { color: 'var(--color-danger)', fontWeight: 700 } : undefined}
          title={isOverLimit ? `Over the WIP limit of ${wipLimit}` : undefined}
        >
          {stories.length}{wipLimit !== undefined && ` / ${wipLimit}`}
        </span>
      </div>
      {stories.map((story) => (
        <StoryCard key={story.id} story={story} teamName={teamName} userNames={userNames} labels={labels} onOpenPanel={onOpenPanel} />
      ))}
    </div>
  )
}
