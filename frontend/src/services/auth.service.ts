import { api, rawApi } from './api'
import type {
  AuthResponse,
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  LoginRequest,
  RefreshRequest,
  RegisterRequest,
  ResetPasswordRequest,
  SignupCompanyRequest,
  User
} from '../types'

export const authService = {
  login: async (data: LoginRequest): Promise<AuthResponse> =>
    (await api.post<AuthResponse>('/auth/login', data)).data,

  signupCompany: async (data: SignupCompanyRequest): Promise<AuthResponse> =>
    (await api.post<AuthResponse>('/auth/signup-company', data)).data,

  register: async (data: RegisterRequest): Promise<AuthResponse> =>
    (await api.post<AuthResponse>('/auth/register', data)).data,

  refresh: async (refreshToken: string): Promise<AuthResponse> =>
    (await api.post<AuthResponse>('/auth/refresh', { refreshToken } satisfies RefreshRequest)).data,

  revoke: async (refreshToken: string): Promise<void> => {
    await rawApi.post('/auth/revoke', { refreshToken } satisfies RefreshRequest)
  },

  forgotPassword: async (email: string): Promise<ForgotPasswordResponse> =>
    (await api.post<ForgotPasswordResponse>('/auth/forgot-password', { email } satisfies ForgotPasswordRequest)).data,

  resetPassword: async (data: ResetPasswordRequest): Promise<void> => {
    await api.post('/auth/reset-password', data)
  },

  me: async (): Promise<User> => (await api.get<User>('/auth/me')).data
}
