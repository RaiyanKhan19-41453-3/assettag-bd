import { useMutation, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { getErrorMessage } from '../services/api'
import { authService } from '../services/auth.service'
import { queryKeys } from '../services/queryKeys'
import type { RegisterRequest } from '../types'

export function useInviteUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: RegisterRequest) => authService.register(data),
    onSuccess: (response) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.users.all })
      toast.success(`Invited ${response.user.fullName}`)
    },
    onError: (error: unknown) => toast.error(getErrorMessage(error))
  })
}
