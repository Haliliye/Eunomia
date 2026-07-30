import { apiClient } from './client'
import type { StoredAuth } from './client'
import type { NotificationPreferences } from '@/types/notificationPreferences'

// Re-exported so existing imports of `AuthResult` elsewhere keep working —
// it's the same shape as StoredAuth now that tokens no longer come back in
// the response body (they're set as httpOnly cookies instead).
export type AuthResult = StoredAuth

export const authApi = {
  register: (email: string, displayName: string, password: string) =>
    apiClient.post<AuthResult>('/auth/register', { email, displayName, password }).then((res) => res.data),

  login: (email: string, password: string) =>
    apiClient.post<AuthResult>('/auth/login', { email, password }).then((res) => res.data),

  // No refresh token param — it rides on the httpOnly cookie automatically.
  refresh: () =>
    apiClient.post<AuthResult>('/auth/refresh').then((res) => res.data),

  // No refresh token param — the backend reads (and clears) the cookie itself.
  logout: () =>
    apiClient.post('/auth/logout'),

  forgotPassword: (email: string) =>
    apiClient.post<{ message: string; devResetToken: string | null }>('/auth/forgot-password', { email })
      .then((res) => res.data),

  resetPassword: (token: string, newPassword: string) =>
    apiClient.post('/auth/reset-password', { token, newPassword }),

  verifyEmail: (token: string) =>
    apiClient.post('/auth/verify-email', { token }),

  resendVerification: () =>
    apiClient.post<{ message: string; devVerificationToken: string | null }>('/auth/resend-verification')
      .then((res) => res.data),
}

// Kept in this file alongside the rest of the "about me" account API rather
// than a separate module — small enough not to need its own file yet.
export const accountApi = {
  getNotificationPreferences: () =>
    apiClient.get<NotificationPreferences>('/users/me/notification-preferences').then((res) => res.data),

  updateNotificationPreferences: (prefs: NotificationPreferences) =>
    apiClient.put('/users/me/notification-preferences', prefs),

  search: (keyword: string) =>
    apiClient.get<GlobalSearchResult[]>('/users/me/search', { params: { keyword } }).then((res) => res.data),
}

export interface GlobalSearchResult {
  storyId: string
  title: string
  teamId: string
  teamName: string
  status: string
}
