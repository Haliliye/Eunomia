import { createContext, useContext, useState, type ReactNode } from 'react'
import { authApi } from '@/api/auth'
import { getStoredAuth, setStoredAuth, clearStoredAuth, type StoredAuth } from '@/api/client'

interface AuthContextValue {
  user: StoredAuth | null
  login: (email: string, password: string) => Promise<void>
  register: (email: string, displayName: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<StoredAuth | null>(getStoredAuth)

  const persist = (result: StoredAuth) => {
    setStoredAuth(result)
    setUser(result)
  }

  const login = async (email: string, password: string) => {
    const result = await authApi.login(email, password)
    persist(result)
  }

  const register = async (email: string, displayName: string, password: string) => {
    const result = await authApi.register(email, displayName, password)
    persist(result)
  }

  const logout = () => {
    // Best-effort — revoke the refresh token server-side (read from the
    // httpOnly cookie, and cleared by the same call) so it can't be used
    // again, but don't block clearing local state on it (e.g. if we're
    // offline, the person should still end up logged out locally).
    authApi.logout().catch(() => {})
    clearStoredAuth()
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within an AuthProvider')
  return context
}
