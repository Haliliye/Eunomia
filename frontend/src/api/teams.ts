import { apiClient } from './client'
import type { Team, Label, StoryTemplate } from '@/types/team'
import type { PagedResult } from '@/types/paged'
import type { Activity } from '@/types/activity'

export type { StoryTemplate }

export const teamsApi = {
  getMyTeams: (page = 1, pageSize = 25) =>
    apiClient.get<PagedResult<Team>>('/teams', { params: { page, pageSize } }).then((res) => res.data),

  getById: (teamId: string) =>
    apiClient.get<Team>(`/teams/${teamId}`).then((res) => res.data),

  create: (name: string, description: string | undefined) =>
    apiClient.post<Team>('/teams', { name, description }).then((res) => res.data),

  update: (teamId: string, name: string, description: string | undefined) =>
    apiClient.put(`/teams/${teamId}`, { name, description }),

  delete: (teamId: string) =>
    apiClient.delete(`/teams/${teamId}`),

  invite: (teamId: string, email: string) =>
    apiClient.post(`/teams/${teamId}/invitations`, { email }),

  removeMember: (teamId: string, userId: string) =>
    apiClient.delete(`/teams/${teamId}/members/${userId}`),

  setMemberRole: (teamId: string, userId: string, role: 'Admin' | 'Member') =>
    apiClient.put(`/teams/${teamId}/members/${userId}/role`, { role }),

  createLabel: (teamId: string, name: string, color: string) =>
    apiClient.post<Label>(`/teams/${teamId}/labels`, { name, color }).then((res) => res.data),

  updateLabel: (teamId: string, labelId: string, name: string, color: string) =>
    apiClient.put(`/teams/${teamId}/labels/${labelId}`, { name, color }),

  deleteLabel: (teamId: string, labelId: string) =>
    apiClient.delete(`/teams/${teamId}/labels/${labelId}`),

  setWipLimit: (teamId: string, status: string, limit: number | null) =>
    apiClient.put(`/teams/${teamId}/wip-limits/${status}`, { limit }),

  createTemplate: (teamId: string, name: string, defaultDescription: string | undefined, defaultPriority: string | undefined, checklistItemTexts: string[]) =>
    apiClient.post<StoryTemplate>(`/teams/${teamId}/templates`, { name, defaultDescription, defaultPriority, checklistItemTexts }).then((res) => res.data),

  deleteTemplate: (teamId: string, templateId: string) =>
    apiClient.delete(`/teams/${teamId}/templates/${templateId}`),

  getActivity: (teamId: string, page = 1, pageSize = 20, actorUserId?: string, actionType?: string) =>
    apiClient.get<PagedResult<Activity>>(`/teams/${teamId}/activity`, { params: { page, pageSize, actorUserId, actionType } })
      .then((res) => res.data),

  getTimeReport: (teamId: string, startDate?: string, endDate?: string) =>
    apiClient.get<TeamTimeReport>(`/teams/${teamId}/time-report`, { params: { startDate, endDate } }).then((res) => res.data),
}

export interface TeamTimeReport {
  rows: { storyId: string; title: string; estimatedHours?: number; loggedHours: number; variance?: number }[]
  totalEstimatedHours: number
  totalLoggedHours: number
}
