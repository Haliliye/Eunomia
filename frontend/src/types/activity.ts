export type ActivityType = 'Created' | 'StatusChanged' | 'Assigned' | 'Archived' | 'Commented'

export interface Activity {
  id: string
  actorUserId: string
  type: ActivityType
  message: string
  relatedEntityId?: string
  createdOn: string
}
