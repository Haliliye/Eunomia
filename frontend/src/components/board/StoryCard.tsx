import { useDraggable } from '@dnd-kit/core'
import type { UserStory } from '@/types/userStory'
import type { Label } from '@/types/team'
import PriorityBadge from '@/components/common/PriorityBadge'
import LabelChip from '@/components/common/LabelChip'
import { ticketCode } from '@/lib/ticketCode'
import { avatarColor } from '@/lib/avatarColor'
import { displayNameOrId, initialsFor } from '@/hooks/useUserNames'
import { isOverdue, formatDueDate } from '@/lib/dueDate'

interface StoryCardProps {
  story: UserStory
  teamName: string
  userNames: Record<string, string>
  labels: Label[]
  onOpenPanel: (story: UserStory) => void
}

export default function StoryCard({ story, teamName, userNames, labels, onOpenPanel }: StoryCardProps) {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: story.id,
  })

  const style = transform
    ? {
        transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`,
        opacity: isDragging ? 0.5 : 1,
        zIndex: isDragging ? 10 : undefined,
      }
    : undefined

  const overdue = isOverdue(story)
  const storyLabels = story.labelIds.map((id) => labels.find((l) => l.id === id)).filter((l): l is Label => Boolean(l))
  const hasBlockingLinks = story.links?.some((l) => l.linkType === 'Blocks' || l.linkType === 'BlockedBy')

  return (
    <div
      className="story-card"
      ref={setNodeRef}
      style={style}
      {...listeners}
      {...attributes}
    >
      <div className="story-card-body">
        <div className="story-card-top">
          <span className="story-ticket-code">
            {ticketCode(teamName, story.id)}
            {story.storyPoints !== undefined && ` · ${story.storyPoints}pt`}
          </span>
          <div style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
            <PriorityBadge priority={story.priority} />
            <button
              className="btn-ghost btn-sm story-card-menu-btn"
              aria-label="Open story panel"
              title="Open"
              onPointerDown={(e) => e.stopPropagation()}
              onClick={(e) => { e.stopPropagation(); onOpenPanel(story) }}
            >
              ⋮
            </button>
          </div>
        </div>
        <div className="story-card-title">
          {story.recurrenceFrequency && <span title={`Repeats ${story.recurrenceFrequency}`}>🔁 </span>}
          {hasBlockingLinks && <span title="Has linked stories">🔗 </span>}
          {story.title}
        </div>
        {storyLabels.length > 0 && (
          <div style={{ display: 'flex', gap: 4, flexWrap: 'wrap', marginTop: 4 }}>
            {storyLabels.map((label) => <LabelChip key={label.id} label={label} />)}
          </div>
        )}
        {story.checklistItems.length > 0 && (
          <div className="story-card-checklist mono">
            ☑ {story.checklistItems.filter((i) => i.isCompleted).length}/{story.checklistItems.length}
            {story.attachments.length > 0 && `  ·  📎 ${story.attachments.length}`}
          </div>
        )}
        {story.checklistItems.length === 0 && story.attachments.length > 0 && (
          <div className="story-card-checklist mono">📎 {story.attachments.length}</div>
        )}
        <div className="story-card-footer">
          {story.dueDate ? (
            <span className={overdue ? 'story-due-date overdue' : 'story-due-date'} title={overdue ? 'Overdue' : 'Due date'}>
              {formatDueDate(story.dueDate)}
            </span>
          ) : (
            <span />
          )}
          {story.assigneeId && (
            <span
              className="assignee-chip"
              style={{ background: avatarColor(story.assigneeId), color: 'white' }}
              title={displayNameOrId(userNames, story.assigneeId)}
            >
              {initialsFor(userNames, story.assigneeId)}
            </span>
          )}
        </div>
      </div>
    </div>
  )
}
