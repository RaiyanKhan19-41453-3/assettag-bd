import { api } from './api'
import type { User, UserRole } from '../types'

export const usersService = {
  list: async (role?: UserRole): Promise<User[]> =>
    (await api.get<User[]>('/users', { params: role ? { role } : {} })).data
}
