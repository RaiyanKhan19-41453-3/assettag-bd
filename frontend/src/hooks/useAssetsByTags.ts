import { useQuery } from '@tanstack/react-query'
import { assetsService } from '../services/assets.service'

const LABEL_TAGS_KEY = 'assettagbd.labelTags'

export function storeLabelTags(tags: string[]): void {
  sessionStorage.setItem(LABEL_TAGS_KEY, JSON.stringify(tags))
}

export function readStoredLabelTags(): string[] {
  try {
    const value = sessionStorage.getItem(LABEL_TAGS_KEY)
    if (!value) return []
    const parsed: unknown = JSON.parse(value)
    return Array.isArray(parsed) ? parsed.filter((tag): tag is string => typeof tag === 'string') : []
  } catch {
    return []
  }
}

export function useAssetsByTags(tags: string[]) {
  return useQuery({
    queryKey: ['assets', 'byTags', [...tags].sort()] as const,
    queryFn: () => assetsService.getByTags(tags),
    enabled: tags.length > 0
  })
}
