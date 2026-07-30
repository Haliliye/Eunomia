export type TeamRole = 'Owner' | 'Admin' | 'Member'

export interface TeamMember {
  userId: string
  role: TeamRole
  joinedOn: string
}

export interface Label {
  id: string
  name: string
  color: string
}

export interface WipLimit {
  status: string
  limit: number
}

export interface StoryTemplate {
  id: string
  name: string
  defaultDescription?: string
  defaultPriority?: string
  checklistItemTexts: string[]
}

export interface Team {
  id: string
  name: string
  description?: string
  members: TeamMember[]
  labels: Label[]
  wipLimits: WipLimit[]
  templates: StoryTemplate[]
}

export interface CreateTeamRequest {
  name: string
  description?: string
}
