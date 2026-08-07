import { apiClient } from './client'

export interface Board {
  id: string
  teamId: string
  name: string
  sprintId?: string
  createdOn: string
}

export const boardsApi = {
  getByTeam: (teamId: string) => apiClient.get<Board[]>(`/teams/${teamId}/boards`).then((res) => res.data),

  create: (teamId: string, name: string, sprintId?: string) =>
    apiClient.post<Board>(`/teams/${teamId}/boards`, { name, sprintId: sprintId || null }).then((res) => res.data),

  rename: (boardId: string, name: string, sprintId?: string) =>
    apiClient.put(`/boards/${boardId}`, { name, sprintId: sprintId || null }),

  delete: (boardId: string) => apiClient.delete(`/boards/${boardId}`),
}
