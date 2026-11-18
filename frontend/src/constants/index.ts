import type { AssetCategory, AssetStatus, UserRole } from '../types'

export const ASSET_CATEGORIES: AssetCategory[] = [
  'Laptop',
  'Monitor',
  'Phone',
  'Furniture',
  'Other'
]

export const ASSET_STATUSES: AssetStatus[] = [
  'Available',
  'Assigned',
  'UnderMaintenance',
  'Retired'
]

export const USER_ROLES: UserRole[] = ['SuperAdmin', 'ITManager', 'Employee']

export const CATEGORY_LABELS: Record<AssetCategory, string> = {
  Laptop: 'Laptop',
  Monitor: 'Monitor',
  Phone: 'Phone',
  Furniture: 'Furniture',
  Other: 'Other'
}

export const STATUS_LABELS: Record<AssetStatus, string> = {
  Available: 'Available',
  Assigned: 'Assigned',
  UnderMaintenance: 'Under maintenance',
  Retired: 'Retired'
}

export const ROLE_LABELS: Record<UserRole, string> = {
  SuperAdmin: 'Super Admin',
  ITManager: 'IT Manager',
  Employee: 'Employee'
}

/** Date inputs require yyyy-MM-dd; API accepts date-only strings and normalises to UTC. */
export function toDateInputValue(iso?: string | null): string {
  if (!iso) return ''
  return iso.slice(0, 10)
}
