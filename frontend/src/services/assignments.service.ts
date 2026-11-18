import { api } from './api'
import type { AssignAssetRequest, Assignment, ReturnAssetRequest } from '../types'

export const assignmentsService = {
  assign: async (data: AssignAssetRequest): Promise<Assignment> =>
    (await api.post<Assignment>('/assignments/assign', data)).data,

  returnAsset: async (data: ReturnAssetRequest): Promise<Assignment> =>
    (await api.post<Assignment>('/assignments/return', data)).data,

  history: async (assetId: string): Promise<Assignment[]> =>
    (await api.get<Assignment[]>(`/assignments/history/${assetId}`)).data
}
