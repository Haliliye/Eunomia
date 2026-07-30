export interface PersonalTask {
  id: string
  title: string
  description?: string
  dueDate?: string
  isCompleted: boolean
  createdOn: string
  convertedToUserStoryId?: string
}

export interface MyWorkItem {
  id: string
  title: string
  sourceType: 'Personal' | 'TeamStory'
  isCompleted: boolean
  dueDate?: string
  teamId?: string
  teamName?: string
}
