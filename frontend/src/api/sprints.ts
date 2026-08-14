import { apiClient } from './client'
import type { Sprint } from '@/types/sprint'

export interface BurndownPoint {
  date: string
  remainingCount: number
  remainingPoints: number
}

export interface SprintBurndown {
  startDate: string
  endDate: string
  totalPointsAtStart: number
  actualSnapshots: BurndownPoint[]
}

export interface CarriedOverStory {
  id: string
  title: string
  status: string
}

export interface SprintCompletionSummary {
  sprintId: string
  sprintName: string
  completedCount: number
  completedPoints: number
  carriedOverCount: number
  carriedOverPoints: number
  carriedOverStories: CarriedOverStory[]
}

export const sprintsApi = {
  getForTeam: (teamId: string) =>
    apiClient.get<Sprint[]>(`/teams/${teamId}/sprints`).then((res) => res.data),

  create: (teamId: string, name: string, startDate: string, endDate: string) =>
    apiClient.post<Sprint>(`/teams/${teamId}/sprints`, { name, startDate, endDate }).then((res) => res.data),

  start: (sprintId: string) =>
    apiClient.put(`/sprints/${sprintId}/start`),

  complete: (sprintId: string) =>
    apiClient.put<SprintCompletionSummary>(`/sprints/${sprintId}/complete`).then((res) => res.data),

  getBurndown: (sprintId: string) =>
    apiClient.get<SprintBurndown>(`/sprints/${sprintId}/burndown`).then((res) => res.data),

  getVelocity: (teamId: string) =>
    apiClient.get<VelocityPoint[]>(`/teams/${teamId}/velocity`).then((res) => res.data),
}

export interface VelocityPoint {
  sprintId: string
  sprintName: string
  endDate: string
  plannedPoints?: number
  completedPoints: number
}
