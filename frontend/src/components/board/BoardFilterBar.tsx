import type { UserStoryPriority } from '@/types/userStory'
import type { TeamMember } from '@/types/team'
import { avatarColor } from '@/lib/avatarColor'
import { initialsFor, displayNameOrId } from '@/hooks/useUserNames'

const ALL_PRIORITIES: UserStoryPriority[] = ['Critical', 'High', 'Medium', 'Low']

export interface BoardFilters {
  keyword?: string
  assigneeId?: string
  priority?: string
}

interface BoardFilterBarProps {
  members: TeamMember[]
  userNames: Record<string, string>
  filters: BoardFilters
  onChange: (filters: BoardFilters) => void
}

// Client-side filtering (the board already loads the whole backlog at once —
// see BoardPage) rather than round-tripping to the server on every keystroke.
export default function BoardFilterBar({ members, userNames, filters, onChange }: BoardFilterBarProps) {
  const hasActiveFilters = Boolean(filters.keyword || filters.assigneeId || filters.priority)

  const toggleAssignee = (userId: string) => {
    onChange({ ...filters, assigneeId: filters.assigneeId === userId ? undefined : userId })
  }

  return (
    <div className="backlog-toolbar">
      <input
        className="backlog-search"
        placeholder="Search board"
        aria-label="Search board"
        value={filters.keyword ?? ''}
        onChange={(e) => onChange({ ...filters, keyword: e.target.value || undefined })}
      />

      <div className="avatar-filter-stack">
        {members.map((m) => (
          <button
            key={m.userId}
            className={`avatar-filter-chip ${filters.assigneeId === m.userId ? 'active' : ''}`}
            style={{ background: avatarColor(m.userId) }}
            title={`Filter by ${displayNameOrId(userNames, m.userId)}`}
            onClick={() => toggleAssignee(m.userId)}
          >
            {initialsFor(userNames, m.userId)}
          </button>
        ))}
      </div>

      <select
        className="pill-select"
        aria-label="Filter by priority"
        value={filters.priority ?? ''}
        onChange={(e) => onChange({ ...filters, priority: e.target.value || undefined })}
      >
        <option value="">Filter: Priority</option>
        {ALL_PRIORITIES.map((p) => (
          <option key={p} value={p}>{p}</option>
        ))}
      </select>

      {hasActiveFilters && (
        <button className="btn btn-sm" onClick={() => onChange({})}>Clear filters</button>
      )}
    </div>
  )
}
