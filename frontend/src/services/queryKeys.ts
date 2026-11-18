import type { AssetFilters } from '../types'

/** Centralised query keys make cache invalidation predictable. */
export const queryKeys = {
  auth: {
    me: ['auth', 'me'] as const
  },
  assets: {
    all: ['assets'] as const,
    mine: ['assets', 'mine'] as const,
    list: (filters: AssetFilters) => ['assets', 'list', filters] as const,
    detail: (id: string) => ['assets', 'detail', id] as const,
    byTag: (tag: string) => ['assets', 'tag', tag] as const
  },
  assignments: {
    all: ['assignments'] as const,
    history: (assetId: string) => ['assignments', 'history', assetId] as const
  },
  maintenance: {
    all: ['maintenance'] as const,
    byAsset: (assetId: string) => ['maintenance', 'asset', assetId] as const
  },
  dashboard: {
    summary: ['dashboard', 'summary'] as const
  },
  users: {
    all: ['users'] as const,
    list: (role?: string) => ['users', 'list', role ?? 'all'] as const
  }
}
