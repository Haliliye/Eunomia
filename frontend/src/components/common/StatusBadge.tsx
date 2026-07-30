import type { UserStoryStatus } from '@/types/userStory'

const LABELS: Record<UserStoryStatus, string> = {
  ToDo: 'To Do',
  Analyze: 'Analyze',
  Dev: 'Dev',
  Test: 'Test',
  Debug: 'Debug',
  Done: 'Done',
}

const CLASS: Record<UserStoryStatus, string> = {
  ToDo: 'badge badge-status-todo',
  Analyze: 'badge badge-status-analyze',
  Dev: 'badge badge-status-dev',
  Test: 'badge badge-status-test',
  Debug: 'badge badge-status-debug',
  Done: 'badge badge-status-done',
}

export default function StatusBadge({ status }: { status: UserStoryStatus }) {
  return <span className={CLASS[status]}>{LABELS[status]}</span>
}
