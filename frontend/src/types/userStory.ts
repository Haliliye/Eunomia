export type UserStoryStatus = 'ToDo' | 'Analyze' | 'Dev' | 'Test' | 'Debug' | 'Done'
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
}
