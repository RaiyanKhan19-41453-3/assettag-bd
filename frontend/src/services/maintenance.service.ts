import { api } from './api'
import type { CreateMaintenanceLogRequest, MaintenanceLog } from '../types'

export const maintenanceService = {
  report: async (data: CreateMaintenanceLogRequest): Promise<MaintenanceLog> =>
    (await api.post<MaintenanceLog>('/maintenance/report', data)).data,

  resolve: async (id: string): Promise<MaintenanceLog> =>
    (await api.post<MaintenanceLog>(`/maintenance/${id}/resolve`)).data,

  byAsset: async (assetId: string): Promise<MaintenanceLog[]> =>
    (await api.get<MaintenanceLog[]>(`/maintenance/asset/${assetId}`)).data
}
