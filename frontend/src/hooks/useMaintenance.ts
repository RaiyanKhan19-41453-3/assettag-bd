import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { getErrorMessage } from '../services/api'
import { maintenanceService } from '../services/maintenance.service'
import { queryKeys } from '../services/queryKeys'
import type { CreateMaintenanceLogRequest } from '../types'

export function useMaintenanceLogs(assetId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.maintenance.byAsset(assetId ?? ''),
    queryFn: () => maintenanceService.byAsset(assetId!),
    enabled: Boolean(assetId)
  })
}

export function useReportMaintenance() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateMaintenanceLogRequest) => maintenanceService.report(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.assets.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.maintenance.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.summary })
      toast.success('Maintenance issue reported')
    },
    onError: (error: unknown) => toast.error(getErrorMessage(error))
  })
}

export function useResolveMaintenance() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => maintenanceService.resolve(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.assets.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.maintenance.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.summary })
      toast.success('Maintenance resolved')
    },
    onError: (error: unknown) => toast.error(getErrorMessage(error))
  })
}
