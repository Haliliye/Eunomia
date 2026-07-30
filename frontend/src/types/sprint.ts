export type SprintStatus = 'Planned' | 'Active' | 'Completed'

export interface Sprint {
  id: string
  teamId: string
  name: string
  startDate: string
  endDate: string
  status: SprintStatus
  createdOn: string
}
