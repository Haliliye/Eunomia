import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'

const AUTH_STORAGE_KEY = 'todoapp:auth'

// Points at the ASP.NET Core API (see backend/src/TodoApp.Api).
// withCredentials so the httpOnly access_token/refresh_token cookies the
// backend sets on login/register/refresh are actually sent back on every
// request — without this, the browser withholds cookies on cross-origin
// calls (frontend and backend run on different ports in dev).
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:5001/api',
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
})

// What's stored here is deliberately non-sensitive — just enough to render
// "who's logged in" without an extra round trip on page load. The actual
// access/refresh tokens never touch localStorage (or any JS-readable
// storage) at all now; they live only in httpOnly cookies the browser
// manages, which an XSS payload running on the page can't read out.
export interface StoredAuth {
  userId: string
  email: string
  displayName: string
  isEmailVerified: boolean
  emailVerificationDevToken?: string | null
}

export function getStoredAuth(): StoredAuth | null {
  try {
    const raw = localStorage.getItem(AUTH_STORAGE_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

export function setStoredAuth(auth: StoredAuth) {
  localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(auth))
}

export function clearStoredAuth() {
  localStorage.removeItem(AUTH_STORAGE_KEY)
}

export function clearStoredAuthAndRedirect() {
  clearStoredAuth()
  if (window.location.pathname !== '/login') {
    window.location.href = '/login'
  }
}

// Access tokens are short-lived (15 min) by design (see backend
// JwtSettings.ExpiryMinutes) — a 401 doesn't necessarily mean "log out",
// it usually just means "this token expired mid-session". So on a 401 we
// try the refresh token once before giving up. Concurrent requests that
// 401 at the same time share a single in-flight refresh call instead of
// each independently hitting /auth/refresh. The refresh call needs no body
// and returns no tokens to store — the browser just receives fresh
// Set-Cookie headers and the retried request rides on those automatically.
let refreshPromise: Promise<void> | null = null

async function refreshAccessToken(): Promise<void> {
  // Raw axios call (not apiClient) so this request's own failure isn't
  // re-intercepted into another refresh attempt.
  await axios.post(`${apiClient.defaults.baseURL}/auth/refresh`, null, { withCredentials: true })
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined

    const isRefreshCall = originalRequest?.url?.includes('/auth/refresh')
    const shouldAttemptRefresh =
      error.response?.status === 401 && originalRequest && !originalRequest._retried && !isRefreshCall

    if (!shouldAttemptRefresh) {
      if (error.response?.status === 401) clearStoredAuthAndRedirect()
      return Promise.reject(error)
    }

    originalRequest._retried = true

    try {
      if (!refreshPromise) {
        refreshPromise = refreshAccessToken().finally(() => {
          refreshPromise = null
        })
      }
      await refreshPromise
      return apiClient(originalRequest)
    } catch {
      clearStoredAuthAndRedirect()
      return Promise.reject(error)
    }
  }
)
