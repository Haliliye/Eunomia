import { apiClient } from './client'
import type { PersonalTask, MyWorkItem } from '@/types/personalTask'
import type { UserStory } from '@/types/userStory'

export const personalTasksApi = {
  getMine: () =>
    apiClient.get<PersonalTask[]>('/personal-tasks').then((res) => res.data),

  create: (title: string, description: string | undefined, dueDate: string | undefined) =>
    apiClient.post<PersonalTask>('/personal-tasks', { title, description, dueDate }).then((res) => res.data),

  update: (id: string, title: string, description: string | undefined, dueDate: string | undefined) =>
    apiClient.put(`/personal-tasks/${id}`, { title, description, dueDate }),

  toggle: (id: string, isCompleted: boolean) =>
    apiClient.put(`/personal-tasks/${id}/toggle`, { isCompleted }),

  delete: (id: string) =>
    apiClient.delete(`/personal-tasks/${id}`),

  convert: (id: string, teamId: string) =>
    apiClient.post<UserStory>(`/personal-tasks/${id}/convert`, { teamId }).then((res) => res.data),

  getMyWork: () =>
    apiClient.get<MyWorkItem[]>('/my-work').then((res) => res.data),
}
