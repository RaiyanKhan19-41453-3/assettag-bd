import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { getErrorMessage } from '../services/api'
import { assetsService } from '../services/assets.service'
import { queryKeys } from '../services/queryKeys'
import type { Asset, AssetFilters, CreateAssetRequest, ImportAssetsResult, UpdateAssetRequest } from '../types'

export function useAssets(filters: AssetFilters = {}) {
  return useQuery({
    queryKey: queryKeys.assets.list(filters),
    queryFn: () => assetsService.list(filters)
  })
}

export function useAsset(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.assets.detail(id ?? ''),
    queryFn: () => assetsService.getById(id!),
    enabled: Boolean(id)
  })
}

export function useAssetByTag(tag: string | null) {
  return useQuery({
    queryKey: queryKeys.assets.byTag(tag ?? ''),
    queryFn: () => assetsService.getByTag(tag!),
    enabled: Boolean(tag),
    retry: false
  })
}

function useInvalidateAssets() {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.assets.all })
    void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard.summary })
  }
}

export function useCreateAsset() {
  const invalidate = useInvalidateAssets()
  return useMutation({
    mutationFn: (data: CreateAssetRequest) => assetsService.create(data),
    onSuccess: (asset: Asset) => {
      invalidate()
      toast.success(`Asset ${asset.assetTag} created`)
    },
    onError: (error: unknown) => toast.error(getErrorMessage(error))
  })
}

export function useUpdateAsset() {
  const invalidate = useInvalidateAssets()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAssetRequest }) => assetsService.update(id, data),
    onSuccess: () => {
      invalidate()
      toast.success('Asset updated')
    },
    onError: (error: unknown) => toast.error(getErrorMessage(error))
  })
}

export function useImportAssets() {
  const invalidate = useInvalidateAssets()
  return useMutation({
    mutationFn: (file: File) => assetsService.import(file),
    onSuccess: (result: ImportAssetsResult) => {
      invalidate()
      toast.success(`Imported ${result.created} asset${result.created === 1 ? '' : 's'}`)
    },
    onError: (error: unknown) => toast.error(getErrorMessage(error))
  })
}

export function useDeleteAsset() {
  const invalidate = useInvalidateAssets()
  return useMutation({
    mutationFn: (id: string) => assetsService.remove(id),
    onSuccess: () => {
      invalidate()
      toast.success('Asset deleted')
    },
    onError: (error: unknown) => toast.error(getErrorMessage(error))
  })
}
