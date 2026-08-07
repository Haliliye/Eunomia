// Was a fixed union — now a per-team board column Key (see BoardColumn on
// the backend). The six original values still exist as every team's default
// columns, but a team can add more, so this can't be a closed union anymore.
export type UserStoryStatus = string
export type UserStoryPriority = 'Low' | 'Medium' | 'High' | 'Critical'

export interface ChecklistItem {
  id: string
  text: string
  isCompleted: boolean
  order: number
}

export interface Attachment {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  uploadedByUserId: string
  uploadedOn: string
}

export interface TimeLogEntry {
  id: string
  hours: number
  note?: string
  loggedByUserId: string
  loggedOn: string
}

export interface StoryLink {
  linkedStoryId: string
  linkType: 'Blocks' | 'BlockedBy' | 'RelatesTo'
}

export interface UserStory {
  id: string
  teamId: string
  title: string
  description?: string
  status: UserStoryStatus
  priority: UserStoryPriority
  assigneeId?: string
  dueDate?: string
  version: number
  isArchived: boolean
  storyPoints?: number
  sprintId?: string
  checklistItems: ChecklistItem[]
  labelIds: string[]
  recurrenceFrequency?: 'Daily' | 'Weekly' | 'Monthly'
  recurrenceEndDate?: string
  attachments: Attachment[]
  estimatedHours?: number
  timeLogEntries: TimeLogEntry[]
  totalLoggedHours: number
  links: StoryLink[]
  createdOn: string
  createdByUserId?: string
  parentId?: string
  epicId?: string
}
