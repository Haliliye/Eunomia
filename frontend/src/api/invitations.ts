import { apiClient } from './client'
import type { Invitation } from '@/types/invitation'

export const invitationsApi = {
  getMine: () =>
    apiClient.get<Invitation[]>('/invitations').then((res) => res.data),

  getForTeam: (teamId: string) =>
    apiClient.get<Invitation[]>(`/teams/${teamId}/invitations`).then((res) => res.data),

  accept: (id: string) =>
    apiClient.put(`/invitations/${id}/accept`),

  decline: (id: string) =>
    apiClient.put(`/invitations/${id}/decline`),

  cancel: (id: string) =>
    apiClient.delete(`/invitations/${id}`),
}
