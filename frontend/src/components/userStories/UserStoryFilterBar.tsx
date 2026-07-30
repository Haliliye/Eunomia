import { useEffect, useState } from 'react'
import type { UserStoryStatus, UserStoryPriority } from '@/types/userStory'
import type { TeamMember } from '@/types/team'
import type { UserStoryFilters } from '@/api/userStories'
import { avatarColor } from '@/lib/avatarColor'
import { displayNameOrId, initialsFor } from '@/hooks/useUserNames'

const ALL_STATUSES: UserStoryStatus[] = ['ToDo', 'Analyze', 'Dev', 'Test', 'Debug', 'Done']
const ALL_PRIORITIES: UserStoryPriority[] = ['Critical', 'High', 'Medium', 'Low']

interface UserStoryFilterBarProps {
  members: TeamMember[]
  userNames: Record<string, string>
  filters: UserStoryFilters
  onChange: (filters: UserStoryFilters) => void
}

// US-115: filter by status/assignee/priority (combinable) + "Clear filters".
// US-116: keyword search across title/description, debounced as-you-type.
// The overlapping avatar row is a quick assignee filter (click to toggle) —
// a common pattern in Jira-style backlogs.
export default function UserStoryFilterBar({ members, userNames, filters, onChange }: UserStoryFilterBarProps) {
  const [keywordInput, setKeywordInput] = useState(filters.keyword ?? '')

  useEffect(() => {
    const timeout = setTimeout(() => {
      if (keywordInput !== (filters.keyword ?? '')) {
        onChange({ ...filters, keyword: keywordInput || undefined })
      }
    }, 300)
    return () => clearTimeout(timeout)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [keywordInput])

  const hasActiveFilters = Boolean(filters.status || filters.priority || filters.assigneeId || filters.keyword)

  const handleClear = () => {
    setKeywordInput('')
    onChange({})
  }

  const toggleAssignee = (userId: string) => {
    onChange({ ...filters, assigneeId: filters.assigneeId === userId ? undefined : userId })
  }

  return (
    <div className="backlog-toolbar">
      <input
        id="backlog-search-input"
        className="backlog-search"
        placeholder="Search backlog (/)"
        aria-label="Search stories"
        value={keywordInput}
        onChange={(e) => setKeywordInput(e.target.value)}
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
        aria-label="Filter by status"
        value={filters.status ?? ''}
        onChange={(e) => onChange({ ...filters, status: e.target.value || undefined })}
      >
        <option value="">Filter: Status</option>
        {ALL_STATUSES.map((s) => (
          <option key={s} value={s}>{s === 'ToDo' ? 'To Do' : s}</option>
        ))}
      </select>

      <select
        className="pill-select"
        aria-label="Filter by priority"
        value={filters.priority ?? ''}
        onChange={(e) => onChange({ ...filters, priority: e.target.value || undefined })}
      >
        <option value="">Custom filters: Priority</option>
        {ALL_PRIORITIES.map((p) => (
          <option key={p} value={p}>{p}</option>
        ))}
      </select>

      {hasActiveFilters && <button className="btn btn-sm" onClick={handleClear}>Clear filters</button>}
    </div>
  )
}
