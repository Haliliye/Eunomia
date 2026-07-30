import type { UserStory } from '@/types/userStory'

// US-121: overdue means the due date has passed AND the story isn't Done —
// a finished item is never "at risk" no matter how late it was closed.
export function isOverdue(story: Pick<UserStory, 'dueDate' | 'status'>): boolean {
  if (!story.dueDate || story.status === 'Done') return false
  return new Date(story.dueDate).getTime() < Date.now()
}

export function formatDueDate(dueDate: string | undefined): string {
  if (!dueDate) return 'No due date'
  return new Date(dueDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}
