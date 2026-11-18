import { Link } from 'react-router-dom'
import {
  AlertTriangle,
  Boxes,
  CheckCircle2,
  ClipboardList,
  Laptop,
  QrCode,
  TrendingUp,
  UserCheck
} from 'lucide-react'
import { RecentActivity } from '../components/dashboard/RecentActivity'
import { StatCard } from '../components/dashboard/StatCard'
import { ErrorState } from '../components/common/ErrorState'
import { LoadingSpinner } from '../components/common/LoadingSpinner'
import { useAuth } from '../context/AuthContext'
import { useDashboardSummary } from '../hooks/useDashboard'

export function DashboardPage() {
  const { data, isLoading, isError, refetch } = useDashboardSummary()
  const { user, hasRole } = useAuth()

  if (isLoading) {
    return <div className="card flex min-h-64 items-center justify-center"><LoadingSpinner label="Loading dashboard…" /></div>
  }

  if (isError || !data) {
    return <ErrorState message="Could not load the dashboard." onRetry={() => void refetch()} />
  }

  const health = data.totalAssets > 0
    ? Math.round((data.availableAssets / data.totalAssets) * 100)
    : 0

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 sm:text-3xl">Dashboard</h1>
        <p className="mt-1 text-sm text-slate-500">
          Welcome back, {user?.fullName?.split(' ')[0]}. Here's the current state of your assets.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
        <StatCard label="Total assets" value={data.totalAssets} icon={Boxes} tone="brand" />
        <StatCard label="Assigned" value={data.assignedAssets} icon={UserCheck} tone="blue" />
        <StatCard label="Under maintenance" value={data.underMaintenanceAssets} icon={AlertTriangle} tone="amber" />
        <StatCard label="Available" value={data.availableAssets} icon={CheckCircle2} tone="green" />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="card p-5 lg:col-span-2">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-base font-bold text-slate-900">Asset health</h2>
              <p className="mt-0.5 text-xs text-slate-500">Breakdown by current status</p>
            </div>
            <TrendingUp className="h-5 w-5 text-slate-400" />
          </div>

          <div className="mt-5 flex h-3 overflow-hidden rounded-full bg-slate-100" aria-hidden="true">
            {data.totalAssets > 0 && (
              <>
                <div className="bg-emerald-500" style={{ width: `${(data.availableAssets / data.totalAssets) * 100}%` }} />
                <div className="bg-blue-500" style={{ width: `${(data.assignedAssets / data.totalAssets) * 100}%` }} />
                <div className="bg-amber-500" style={{ width: `${(data.underMaintenanceAssets / data.totalAssets) * 100}%` }} />
                <div className="bg-slate-400" style={{ width: `${(data.retiredAssets / data.totalAssets) * 100}%` }} />
              </>
            )}
          </div>

          <div className="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
            {[
              { label: 'Available', value: data.availableAssets, dot: 'bg-emerald-500' },
              { label: 'Assigned', value: data.assignedAssets, dot: 'bg-blue-500' },
              { label: 'Maintenance', value: data.underMaintenanceAssets, dot: 'bg-amber-500' },
              { label: 'Retired', value: data.retiredAssets, dot: 'bg-slate-400' }
            ].map((item) => (
              <div key={item.label} className="rounded-lg border border-slate-100 p-3">
                <div className="flex items-center gap-2 text-xs text-slate-500">
                  <span className={`h-2 w-2 rounded-full ${item.dot}`} />
                  {item.label}
                </div>
                <p className="mt-1 text-lg font-bold text-slate-900">{item.value}</p>
              </div>
            ))}
          </div>

          <p className="mt-4 text-xs text-slate-500">
            <strong className="text-slate-700">{health}%</strong> of assets are currently available for assignment.
          </p>
        </div>

        <div className="card overflow-hidden">
          <div className="flex items-center gap-2 border-b border-slate-200 px-5 py-4">
            <ClipboardList className="h-4 w-4 text-slate-400" />
            <h2 className="text-base font-bold text-slate-900">Recent activity</h2>
          </div>
          <RecentActivity activities={data.recentActivity} />
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <div className="card flex items-center gap-4 p-5">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-50 text-blue-600">
            <UserCheck className="h-5 w-5" />
          </div>
          <div>
            <p className="text-xl font-bold text-slate-900">{data.activeAssignments}</p>
            <p className="text-xs text-slate-500">Active assignments</p>
          </div>
        </div>
        <div className="card flex items-center gap-4 p-5">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-amber-50 text-amber-600">
            <AlertTriangle className="h-5 w-5" />
          </div>
          <div>
            <p className="text-xl font-bold text-slate-900">{data.openMaintenanceTickets}</p>
            <p className="text-xs text-slate-500">Open maintenance tickets</p>
          </div>
        </div>
        <div className="card flex items-center gap-4 p-5">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
            <Laptop className="h-5 w-5" />
          </div>
          <div>
            <p className="text-xl font-bold text-slate-900">{data.retiredAssets}</p>
            <p className="text-xs text-slate-500">Retired assets</p>
          </div>
        </div>
      </div>

      {hasRole('ITManager', 'SuperAdmin') && (
        <div className="flex flex-col gap-3 rounded-xl border border-brand-100 bg-brand-50/60 p-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-sm font-bold text-slate-900">Ready to check out an asset?</h2>
            <p className="mt-0.5 text-xs text-slate-600">Scan its QR tag to assign or return it in seconds.</p>
          </div>
          <Link to="/scan" className="btn-primary shrink-0">
            <QrCode className="h-4 w-4" />
            Open scanner
          </Link>
        </div>
      )}
    </div>
  )
}
