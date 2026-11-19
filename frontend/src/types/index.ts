export type UserRole = 'SuperAdmin' | 'ITManager' | 'Employee'

export type AssetCategory = 'Laptop' | 'Monitor' | 'Phone' | 'Furniture' | 'Other'

export type AssetStatus = 'Available' | 'Assigned' | 'UnderMaintenance' | 'Retired'

export type MaintenanceStatus = 'Reported' | 'InProgress' | 'Resolved'

export interface User {
  id: string
  fullName: string
  email: string
  role: UserRole
  department?: string | null
}

export interface AuthResponse {
  token: string
  refreshToken: string
  expiresAt: string
  user: User
}

export interface RefreshRequest {
  refreshToken: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ForgotPasswordResponse {
  message: string
  devToken?: string | null
}

export interface ResetPasswordRequest {
  email: string
  token: string
  newPassword: string
}

export interface Asset {
  id: string
  assetTag: string
  name: string
  category: AssetCategory
  serialNumber?: string | null
  purchaseDate: string
  warrantyExpiryDate?: string | null
  status: AssetStatus
  createdAt: string
  currentAssigneeName?: string | null
  assignedDate?: string | null
  qrCodeBase64?: string | null
}

export interface Assignment {
  id: string
  assetId: string
  assetTag: string
  assetName: string
  assignedToUserId: string
  assignedToUserName: string
  assignedByUserId: string
  assignedByUserName: string
  assignedDate: string
  returnedDate?: string | null
  conditionNotes?: string | null
}

export interface MaintenanceLog {
  id: string
  assetId: string
  assetTag: string
  reportedByUserId: string
  reportedByName: string
  issueDescription: string
  status: MaintenanceStatus
  createdAt: string
  resolvedAt?: string | null
}

export interface Activity {
  id: string
  type: 'Assignment' | 'Return' | 'Maintenance'
  description: string
  occurredAt: string
}

export interface DashboardSummary {
  totalAssets: number
  availableAssets: number
  assignedAssets: number
  underMaintenanceAssets: number
  retiredAssets: number
  activeAssignments: number
  openMaintenanceTickets: number
  recentActivity: Activity[]
}

export interface ApiErrorResponse {
  code?: string
  message?: string
  traceId?: string
  errors?: Record<string, string[]>
}

export interface LoginRequest {
  email: string
  password: string
}

export interface SignupCompanyRequest {
  companyName: string
  adminFullName: string
  adminEmail: string
  password: string
}

export interface RegisterRequest {
  fullName: string
  email: string
  password: string
  role: UserRole
  department?: string | null
}

export interface CreateAssetRequest {
  assetTag?: string | null
  name: string
  category: AssetCategory
  serialNumber?: string | null
  purchaseDate: string
  warrantyExpiryDate?: string | null
}

export interface UpdateAssetRequest extends CreateAssetRequest {
  status: AssetStatus
}

export interface AssignAssetRequest {
  assetId: string
  assignedToUserId: string
  conditionNotes?: string | null
}

export interface ReturnAssetRequest {
  assetId: string
  conditionNotes?: string | null
}

export interface CreateMaintenanceLogRequest {
  assetId: string
  issueDescription: string
}

export interface AuditEntry {
  id: string
  action: string
  entityType: string
  entityId?: string | null
  actor: string
  details?: string | null
  occurredAt: string
}

export interface PagedResult<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ImportAssetsResult {
  created: number
  skipped: number
  errors: string[]
  assets: Asset[]
}

export interface AssetFilters {
  category?: AssetCategory
  status?: AssetStatus
  search?: string
  pageNumber?: number
  pageSize?: number
  sortBy?: string
  descending?: boolean
}
