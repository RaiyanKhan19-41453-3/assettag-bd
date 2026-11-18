import { useQuery } from '@tanstack/react-query'
import { usersService } from '../services/users.service'
import { queryKeys } from '../services/queryKeys'
import type { UserRole } from '../types'

export function useUsers(role?: UserRole) {
  return useQuery({
    queryKey: queryKeys.users.list(role),
    queryFn: () => usersService.list(role)
  })
}
