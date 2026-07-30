export interface Comment {
  id: string
  userStoryId: string
  authorId: string
  content: string
  mentionedUserIds: string[]
  createdOn: string
  editedOn?: string
}
