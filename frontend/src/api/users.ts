import { apiClient } from './client'
import type { UserSummary } from '@/types/user'

export const usersApi = {
  getByIds: (ids: string[]) => {
    if (ids.length === 0) return Promise.resolve<UserSummary[]>([])
    return apiClient.get<UserSummary[]>('/users', { params: { ids: ids.join(',') } }).then((res) => res.data)
  },
}
