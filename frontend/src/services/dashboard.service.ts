import { api } from './api'
import type { DashboardSummary } from '../types'

export const dashboardService = {
  summary: async (): Promise<DashboardSummary> =>
    (await api.get<DashboardSummary>('/dashboard/summary')).data
}
