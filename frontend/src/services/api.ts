import axios, { AxiosError } from 'axios'
import type { InternalAxiosRequestConfig } from 'axios'
import type { ApiErrorResponse, AuthResponse, RefreshRequest } from '../types'

export const TOKEN_KEY = 'assettagbd.token'
export const REFRESH_TOKEN_KEY = 'assettagbd.refreshToken'

const API_URL: string = import.meta.env.VITE_API_URL ?? 'http://localhost:5080/api'

/** Application-level error carrying the API's error code and field details. */
export class ApiError extends Error {
  readonly code: string
  readonly status: number
  readonly details?: Record<string, string[]>

  constructor(message: string, code: string, status: number, details?: Record<string, string[]>) {
    super(message)
    this.name = 'ApiError'
    this.code = code
    this.status = status
    this.details = details
  }
}

// No global Content-Type: axios sets application/json for JSON bodies and
// multipart/form-data (with the correct boundary) for FormData uploads.
export const api = axios.create({
  baseURL: API_URL,
  timeout: 30_000
})

// Client without interceptors. Used for token refresh (avoids recursion) and
// for logout revocation (must work after access-token removal).
export const rawApi = axios.create({ baseURL: API_URL, timeout: 15_000 })
const refreshClient = rawApi

const AUTH_PATHS = [
  '/auth/login',
  '/auth/refresh',
  '/auth/revoke',
  '/auth/register',
  '/auth/signup-company',
  '/auth/forgot-password',
  '/auth/reset-password'
]

type RetryConfig = InternalAxiosRequestConfig & { _retry?: boolean }

let refreshPromise: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY)
  if (!refreshToken) return null

  try {
    const payload: RefreshRequest = { refreshToken }
    const response = await refreshClient.post<AuthResponse>('/auth/refresh', payload)
    localStorage.setItem(TOKEN_KEY, response.data.token)
    localStorage.setItem(REFRESH_TOKEN_KEY, response.data.refreshToken)
    return response.data.token
  } catch {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
    return null
  }
}

api.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY)
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiErrorResponse>) => {
    const original = error.config as RetryConfig | undefined

    if (
      original &&
      error.response?.status === 401 &&
      !original._retry &&
      !AUTH_PATHS.some((path) => original.url?.includes(path))
    ) {
      original._retry = true
      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null
      })

      const newToken = await refreshPromise
      if (newToken) {
        original.headers.Authorization = `Bearer ${newToken}`
        return api(original)
      }
    }

    const PUBLIC_PAGES = ['/login', '/signup', '/forgot-password', '/reset-password']
    if (error.response?.status === 401 && !AUTH_PATHS.some((path) => original?.url?.includes(path))) {
      localStorage.removeItem(TOKEN_KEY)
      localStorage.removeItem(REFRESH_TOKEN_KEY)
      if (!PUBLIC_PAGES.some((path) => window.location.pathname.startsWith(path))) {
        window.location.assign('/login')
      }
    }

    const payload = error.response?.data
    const message =
      payload?.message ??
      (typeof error.message === 'string' && error.message !== 'Request failed with status code 401'
        ? error.message
        : 'Your session has expired. Please sign in again.')

    return Promise.reject(
      new ApiError(message, payload?.code ?? 'UnknownError', error.response?.status ?? 0, payload?.errors)
    )
  }
)

export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.details && Object.keys(error.details).length > 0) {
      const fields = Object.entries(error.details)
        .map(([field, messages]) => `${field}: ${messages.join(', ')}`)
        .join(' · ')
      return `${error.message} ${fields}`.trim()
    }
    return error.message
  }
  if (error instanceof Error) return error.message
  return 'Something went wrong. Please try again.'
}
