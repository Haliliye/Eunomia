import type { UserStoryPriority } from '@/types/userStory'

const CLASS: Record<UserStoryPriority, string> = {
  Critical: 'badge badge-priority-critical',
  High: 'badge badge-priority-high',
  Medium: 'badge badge-priority-medium',
  Low: 'badge badge-priority-low',
}

export default function PriorityBadge({ priority }: { priority: UserStoryPriority }) {
  return <span className={CLASS[priority]}>{priority}</span>
}
