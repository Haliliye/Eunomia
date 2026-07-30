import { apiClient } from './client'
import type { Notification } from '@/types/notification'

export const notificationsApi = {
  getMine: () =>
    apiClient.get<Notification[]>('/notifications').then((res) => res.data),

  markRead: (id: string) =>
    apiClient.put(`/notifications/${id}/read`),

  markAllRead: () =>
    apiClient.put('/notifications/read-all'),
}
