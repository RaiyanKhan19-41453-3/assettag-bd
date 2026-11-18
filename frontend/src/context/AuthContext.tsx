import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState
} from 'react'
import type { ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { REFRESH_TOKEN_KEY, TOKEN_KEY } from '../services/api'
import { authService } from '../services/auth.service'
import type { LoginRequest, SignupCompanyRequest, User, UserRole } from '../types'

interface AuthContextValue {
  user: User | null
  isLoading: boolean
  isAuthenticated: boolean
  login: (data: LoginRequest) => Promise<void>
  signupCompany: (data: SignupCompanyRequest) => Promise<void>
  logout: () => void
  hasRole: (...roles: UserRole[]) => boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const queryClient = useQueryClient()

  // Restore the session from a stored JWT on first load.
  useEffect(() => {
    let active = true

    // With only a refresh token left, /auth/me still works: the 401 interceptor
    // silently refreshes and retries the request.
    if (!localStorage.getItem(TOKEN_KEY) && !localStorage.getItem(REFRESH_TOKEN_KEY)) {
      setIsLoading(false)
      return
    }

    authService
      .me()
      .then((currentUser) => {
        if (active) setUser(currentUser)
      })
      .catch(() => {
        // StrictMode runs this effect twice; a stale failure must not wipe a
        // session that a later attempt already restored.
        if (!active) return
        localStorage.removeItem(TOKEN_KEY)
        localStorage.removeItem(REFRESH_TOKEN_KEY)
      })
      .finally(() => {
        if (active) setIsLoading(false)
      })

    return () => {
      active = false
    }
  }, [])

  const login = useCallback(
    async (data: LoginRequest) => {
      const response = await authService.login(data)
      localStorage.setItem(TOKEN_KEY, response.token)
      localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken)
      setUser(response.user)
      queryClient.clear()
    },
    [queryClient]
  )

  const signupCompany = useCallback(
    async (data: SignupCompanyRequest) => {
      const response = await authService.signupCompany(data)
      localStorage.setItem(TOKEN_KEY, response.token)
      localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken)
      setUser(response.user)
      queryClient.clear()
    },
    [queryClient]
  )

  const logout = useCallback(() => {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY)
    if (refreshToken) {
      void authService.revoke(refreshToken).catch(() => undefined)
    }
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
    setUser(null)
    queryClient.clear()
  }, [queryClient])

  const hasRole = useCallback(
    (...roles: UserRole[]) => (user ? roles.includes(user.role) : false),
    [user]
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isLoading,
      isAuthenticated: user !== null,
      login,
      signupCompany,
      logout,
      hasRole
    }),
    [user, isLoading, login, signupCompany, logout, hasRole]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used inside <AuthProvider>')
  }
  return context
}
