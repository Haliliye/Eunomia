import { Link } from 'react-router-dom'
import type { UserStory, UserStoryStatus, UserStoryPriority } from '@/types/userStory'
import type { TeamMember, Label, BoardColumn } from '@/types/team'
import { ticketCode } from '@/lib/ticketCode'
import { avatarColor } from '@/lib/avatarColor'
import { displayNameOrId, initialsFor } from '@/hooks/useUserNames'
import { isOverdue, formatDueDate } from '@/lib/dueDate'
import LabelChip from '@/components/common/LabelChip'

const ALL_PRIORITIES: UserStoryPriority[] = ['Critical', 'High', 'Medium', 'Low']
// Only the six default columns get a distinct pill color, same reasoning as
// StatusBadge — a team-added custom column falls back to a neutral style.
const STATUS_CLASS: Record<string, string> = {
  ToDo: 'backlog-status-pill todo',
  Analyze: 'backlog-status-pill analyze',
  Dev: 'backlog-status-pill dev',
  Test: 'backlog-status-pill test',
  Debug: 'backlog-status-pill debug',
  Done: 'backlog-status-pill done',
}
const PRIORITY_ICON_CLASS: Record<UserStoryPriority, string> = {
  Critical: 'backlog-type-icon critical',
  High: 'backlog-type-icon high',
  Medium: 'backlog-type-icon medium',
  Low: 'backlog-type-icon low',
}

interface UserStoryListProps {
  teamName: string
  stories: UserStory[]
  members: TeamMember[]
  labels: Label[]
  columns: BoardColumn[]
  userNames: Record<string, string>
  onEdit: (story: UserStory) => void
  onDelete: (story: UserStory) => void
  onArchive?: (story: UserStory) => void
  onStatusChange: (story: UserStory, status: UserStoryStatus) => void
  onPriorityChange: (story: UserStory, priority: UserStoryPriority) => void
  onAssigneeChange: (story: UserStory, assigneeId: string | null) => void
  selectedIds?: Set<string>
  onToggleSelect?: (storyId: string) => void
}

// Styled to sit close to a Jira backlog row: type icon (priority-coded),
// ticket key, title, status pill, assignee avatar — one line per story.
export default function UserStoryList({
  teamName,
  stories,
  members,
  labels,
  columns,
  userNames,
  onEdit,
  onDelete,
  onArchive,
  onStatusChange,
  onPriorityChange,
  onAssigneeChange,
  selectedIds,
  onToggleSelect,
}: UserStoryListProps) {
  if (stories.length === 0) {
    return (
      <div className="empty-state">
        <div className="empty-state-title">No user stories match</div>
        <p>Create one, or clear your filters to see the full backlog.</p>
      </div>
    )
  }

  return (
    <div className="backlog-list">
      {stories.map((story) => (
        <div className="backlog-row" key={story.id}>
          {onToggleSelect && (
            <input
              type="checkbox"
              checked={selectedIds?.has(story.id) ?? false}
              onChange={() => onToggleSelect(story.id)}
              aria-label={`Select ${story.title}`}
            />
          )}

          <span
            className={PRIORITY_ICON_CLASS[story.priority]}
            title={`${story.priority} priority`}
            aria-hidden="true"
          >
            ●
          </span>

          <span className="backlog-key">{ticketCode(teamName, story.id)}</span>
          {story.storyPoints !== undefined && (
            <span className="mono" title="Story points" style={{ fontSize: 11, color: 'var(--color-ink-faint)', border: '1px solid var(--color-border-strong)', borderRadius: 4, padding: '1px 6px' }}>
              {story.storyPoints}
            </span>
          )}

          <Link className="backlog-title" to={`/teams/${story.teamId}/stories/${story.id}`}>
            {story.recurrenceFrequency && <span title={`Repeats ${story.recurrenceFrequency}`}>🔁 </span>}
            {story.title}
          </Link>

          {story.labelIds.length > 0 && (
            <div style={{ display: 'flex', gap: 4 }}>
              {story.labelIds.map((labelId) => {
                const label = labels.find((l) => l.id === labelId)
                return label ? <LabelChip key={labelId} label={label} /> : null
              })}
            </div>
          )}

          {story.attachments.length > 0 && (
            <span className="mono" title={`${story.attachments.length} attachment(s)`} style={{ fontSize: 11, color: 'var(--color-ink-faint)' }}>
              📎 {story.attachments.length}
            </span>
          )}

          {story.dueDate && (
            <span className={isOverdue(story) ? 'backlog-due-date overdue' : 'backlog-due-date'}>
              {formatDueDate(story.dueDate)}
            </span>
          )}

          <select
            className="story-inline-select"
            aria-label="Priority"
            value={story.priority}
            onChange={(e) => onPriorityChange(story, e.target.value as UserStoryPriority)}
          >
            {ALL_PRIORITIES.map((p) => (
              <option key={p} value={p}>{p}</option>
            ))}
          </select>

          <div style={{ position: 'relative' }}>
            <select
              className={STATUS_CLASS[story.status] ?? 'backlog-status-pill'}
              aria-label="Status"
              value={story.status}
              onChange={(e) => onStatusChange(story, e.target.value as UserStoryStatus)}
              style={{ appearance: 'none', WebkitAppearance: 'none' }}
            >
              {columns.map((c) => (
                <option key={c.key} value={c.key}>{c.name}</option>
              ))}
            </select>
          </div>

          <span
            className="backlog-avatar"
            style={story.assigneeId ? { background: avatarColor(story.assigneeId), color: 'white' } : undefined}
            title={story.assigneeId ? displayNameOrId(userNames, story.assigneeId) : 'Unassigned'}
          >
            {story.assigneeId ? initialsFor(userNames, story.assigneeId) : '—'}
          </span>
          <select
            className="story-inline-select"
            aria-label="Assignee"
            value={story.assigneeId ?? ''}
            onChange={(e) => onAssigneeChange(story, e.target.value || null)}
          >
            <option value="">Unassigned</option>
            {members.map((m) => (
              <option key={m.userId} value={m.userId}>{displayNameOrId(userNames, m.userId)}</option>
            ))}
          </select>

          <div className="backlog-row-actions">
            <button className="btn btn-ghost btn-sm" onClick={() => onEdit(story)} aria-label="Edit story">✎</button>
            {onArchive && (
              <button className="btn btn-ghost btn-sm" onClick={() => onArchive(story)} aria-label="Archive story" title="Archive">📦</button>
            )}
            <button className="btn btn-ghost btn-sm" onClick={() => onDelete(story)} aria-label="Delete story">✕</button>
          </div>
        </div>
      ))}
    </div>
  )
}
