export type NotificationType = 'Assignment' | 'Mention' | 'TeamInvitation' | 'InvitationAccepted' | 'DueSoon'

export interface Notification {
  id: string
  type: NotificationType
  message: string
  relatedEntityId: string
  isRead: boolean
  createdOn: string
}
