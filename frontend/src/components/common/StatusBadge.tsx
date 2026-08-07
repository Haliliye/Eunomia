import type { UserStoryStatus } from '@/types/userStory'

// Only the six default columns get a distinct color — a team-added custom
// column (see BoardColumn on the backend) falls back to a neutral badge
// style, since there's no way to know in advance what color would suit it.
const LABELS: Record<string, string> = {
  ToDo: 'To Do',
  Analyze: 'Analyze',
  Dev: 'Dev',
  Test: 'Test',
  Debug: 'Debug',
  Done: 'Done',
}

const CLASS: Record<string, string> = {
  ToDo: 'badge badge-status-todo',
  Analyze: 'badge badge-status-analyze',
  Dev: 'badge badge-status-dev',
  Test: 'badge badge-status-test',
  Debug: 'badge badge-status-debug',
  Done: 'badge badge-status-done',
}

interface StatusBadgeProps {
  status: UserStoryStatus
  // The team's display name for this column (team.columns[].name) — pass
  // this whenever the caller has the team loaded, so a renamed default
  // column (or a custom one) shows its real label instead of the raw key.
  label?: string
}

export default function StatusBadge({ status, label }: StatusBadgeProps) {
  return <span className={CLASS[status] ?? 'badge'}>{label ?? LABELS[status] ?? status}</span>
}
