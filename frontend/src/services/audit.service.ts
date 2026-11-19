import { api } from './api'
import type { AuditEntry, PagedResult } from '../types'

export const auditService = {
  list: async (pageNumber = 1, pageSize = 20): Promise<PagedResult<AuditEntry>> =>
    (await api.get<PagedResult<AuditEntry>>('/audit', { params: { pageNumber, pageSize } })).data
}
