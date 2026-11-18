import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { getErrorMessage } from '../services/api'
import { assignmentsService } from '../services/assignments.service'
import { queryKeys } from '../services/queryKeys'
import type { AssignAssetRequest, ReturnAssetRequest } from '../types'

export function useAssignmentHistory(assetId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.assignments.history(assetId ?? ''),
    queryFn: () => assignmentsService.history(assetId!),
    enabled: Boolean(assetId)
  })
}

export function useAssignAsset() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: AssignAssetRequest) => assignmentsService.assign(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.assets.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.assignments.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.summary })
      toast.success('Asset assigned successfully')
    },
    onError: (error: unknown) => toast.error(getErrorMessage(error))
  })
}

export function useReturnAsset() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: ReturnAssetRequest) => assignmentsService.returnAsset(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.assets.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.assignments.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.summary })
      toast.success('Asset returned successfully')
    },
    onError: (error: unknown) => toast.error(getErrorMessage(error))
  })
}
