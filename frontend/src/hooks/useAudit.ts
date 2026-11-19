import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { auditService } from '../services/audit.service'

export function useAudit(pageSize = 20) {
  const [pageNumber, setPageNumber] = useState(1)

  const query = useQuery({
    queryKey: ['audit', pageNumber, pageSize] as const,
    queryFn: () => auditService.list(pageNumber, pageSize)
  })

  return { ...query, pageNumber, setPageNumber }
}

export function formatAction(action: string): string {
  return action.replace(/([a-z])([A-Z])/g, '$1 $2')
}
