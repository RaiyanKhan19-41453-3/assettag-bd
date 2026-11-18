import { api } from './api'
import type {
  Asset,
  AssetFilters,
  CreateAssetRequest,
  ImportAssetsResult,
  PagedResult,
  UpdateAssetRequest
} from '../types'

export const assetsService = {
  list: async (filters: AssetFilters = {}): Promise<PagedResult<Asset>> =>
    (await api.get<PagedResult<Asset>>('/assets', { params: filters })).data,

  mine: async (): Promise<Asset[]> =>
    (await api.get<Asset[]>('/assets/mine')).data,

  getByTags: async (tags: string[]): Promise<Asset[]> =>
    (await api.post<Asset[]>('/assets/by-tags', { tags })).data,

  getById: async (id: string): Promise<Asset> =>
    (await api.get<Asset>(`/assets/${id}`)).data,

  getByTag: async (tag: string): Promise<Asset> =>
    (await api.get<Asset>(`/assets/tag/${encodeURIComponent(tag)}`)).data,

  create: async (data: CreateAssetRequest): Promise<Asset> =>
    (await api.post<Asset>('/assets', data)).data,

  import: async (file: File): Promise<ImportAssetsResult> => {
    const formData = new FormData()
    formData.append('file', file)
    return (await api.post<ImportAssetsResult>('/assets/import', formData)).data
  },

  update: async (id: string, data: UpdateAssetRequest): Promise<Asset> =>
    (await api.put<Asset>(`/assets/${id}`, data)).data,

  remove: async (id: string): Promise<void> => {
    await api.delete(`/assets/${id}`)
  }
}
