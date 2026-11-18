import type { AssetStatus, MaintenanceStatus } from '../../types'

const assetStyles: Record<AssetStatus, string> = {
  Available: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20',
  Assigned: 'bg-blue-50 text-blue-700 ring-blue-600/20',
  UnderMaintenance: 'bg-amber-50 text-amber-700 ring-amber-600/20',
  Retired: 'bg-slate-100 text-slate-600 ring-slate-500/20'
}

const assetLabels: Record<AssetStatus, string> = {
  Available: 'Available',
  Assigned: 'Assigned',
  UnderMaintenance: 'Under maintenance',
  Retired: 'Retired'
}

const maintenanceStyles: Record<MaintenanceStatus, string> = {
  Reported: 'bg-amber-50 text-amber-700 ring-amber-600/20',
  InProgress: 'bg-blue-50 text-blue-700 ring-blue-600/20',
  Resolved: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20'
}

const maintenanceLabels: Record<MaintenanceStatus, string> = {
  Reported: 'Reported',
  InProgress: 'In progress',
  Resolved: 'Resolved'
}

const base = 'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset'

export function AssetStatusBadge({ status }: { status: AssetStatus }) {
  return <span className={`${base} ${assetStyles[status]}`}>{assetLabels[status]}</span>
}

export function MaintenanceStatusBadge({ status }: { status: MaintenanceStatus }) {
  return <span className={`${base} ${maintenanceStyles[status]}`}>{maintenanceLabels[status]}</span>
}
