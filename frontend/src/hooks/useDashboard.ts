import { useQuery } from '@tanstack/react-query'
import { dashboardService } from '../services/dashboard.service'
import { queryKeys } from '../services/queryKeys'

export function useDashboardSummary() {
  return useQuery({
    queryKey: queryKeys.dashboard.summary,
    queryFn: () => dashboardService.summary()
  })
}
