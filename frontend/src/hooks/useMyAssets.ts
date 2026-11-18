import { useQuery } from '@tanstack/react-query'
import { assetsService } from '../services/assets.service'
import { queryKeys } from '../services/queryKeys'

export function useMyAssets() {
  return useQuery({
    queryKey: queryKeys.assets.mine,
    queryFn: () => assetsService.mine()
  })
}
