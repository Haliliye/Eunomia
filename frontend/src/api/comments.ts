import { apiClient } from './client'
import type { Comment } from '@/types/comment'

export const commentsApi = {
  getByUserStory: (userStoryId: string) =>
    apiClient.get<Comment[]>('/comments', { params: { userStoryId } }).then((res) => res.data),

  add: (userStoryId: string, content: string, mentionedUserIds: string[]) =>
    apiClient.post<Comment>('/comments', { userStoryId, content, mentionedUserIds })
      .then((res) => res.data),

  update: (id: string, content: string, mentionedUserIds: string[]) =>
    apiClient.put<Comment>(`/comments/${id}`, { content, mentionedUserIds }).then((res) => res.data),

  delete: (id: string) =>
    apiClient.delete(`/comments/${id}`),
}
