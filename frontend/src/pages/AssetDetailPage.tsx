import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  ArrowLeft,
  CalendarDays,
  CheckCircle2,
  Download,
  Pencil,
  Printer,
  QrCode,
  RotateCcw,
  Trash2,
  UserPlus,
  Wrench
} from 'lucide-react'
import { AssetFormModal } from '../components/assets/AssetFormModal'
import { AssignAssetModal } from '../components/assets/AssignAssetModal'
import { ConfirmDialog } from '../components/common/ConfirmDialog'
import { EmptyState } from '../components/common/EmptyState'
import { ErrorState } from '../components/common/ErrorState'
import { LoadingSpinner } from '../components/common/LoadingSpinner'
import { Modal } from '../components/common/Modal'
import { QRCodeImage } from '../components/common/QRCodeImage'
import {
  AssetStatusBadge,
  MaintenanceStatusBadge
} from '../components/common/StatusBadge'
import { CATEGORY_LABELS, STATUS_LABELS } from '../constants'
import { useAuth } from '../context/AuthContext'
import { useAsset, useDeleteAsset, useUpdateAsset } from '../hooks/useAssets'
import { useAssignmentHistory as useAssetHistory, useAssignAsset, useReturnAsset } from '../hooks/useAssignments'
import {
  useMaintenanceLogs,
  useReportMaintenance,
  useResolveMaintenance
} from '../hooks/useMaintenance'
import type { UpdateAssetRequest } from '../types'

function formatDate(value?: string | null): string {
  if (!value) return '—'
  return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

function formatDateTime(value: string): string {
  return new Date(value).toLocaleString(undefined, {
    year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
  })
}

export function AssetDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { hasRole } = useAuth()

  const { data: asset, isLoading, isError, refetch } = useAsset(id)
  const { data: history } = useAssetHistory(id)
  const { data: logs } = useMaintenanceLogs(id)

  const [assignOpen, setAssignOpen] = useState(false)
  const [editOpen, setEditOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [reportOpen, setReportOpen] = useState(false)
  const [issue, setIssue] = useState('')

  const assignMutation = useAssignAsset()
  const returnMutation = useReturnAsset()
  const reportMutation = useReportMaintenance()
  const resolveMutation = useResolveMaintenance()
  const updateMutation = useUpdateAsset()
  const deleteMutation = useDeleteAsset()

  const canManage = hasRole('ITManager', 'SuperAdmin')
  const canDelete = hasRole('SuperAdmin')

  if (isLoading) {
    return <div className="card flex min-h-64 items-center justify-center"><LoadingSpinner label="Loading asset…" /></div>
  }

  if (isError || !asset) {
    return (
      <div className="space-y-4">
        <Link to="/assets" className="inline-flex items-center gap-1.5 text-sm text-brand-600 hover:underline">
          <ArrowLeft className="h-4 w-4" /> Back to assets
        </Link>
        <ErrorState message="This asset could not be loaded." onRetry={() => void refetch()} />
      </div>
    )
  }

  const downloadQr = () => {
    if (!asset.qrCodeBase64) return
    const link = document.createElement('a')
    link.href = `data:image/png;base64,${asset.qrCodeBase64}`
    link.download = `${asset.assetTag}.png`
    link.click()
  }

  const handleReport = async (event: FormEvent) => {
    event.preventDefault()
    if (!issue.trim()) return
    await reportMutation.mutateAsync({ assetId: asset.id, issueDescription: issue.trim() })
    setIssue('')
    setReportOpen(false)
  }

  return (
    <div className="space-y-6">
      <Link to="/assets" className="inline-flex items-center gap-1.5 text-sm text-brand-600 hover:underline">
        <ArrowLeft className="h-4 w-4" /> Back to assets
      </Link>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-3">
            <h1 className="text-2xl font-bold text-slate-900 sm:text-3xl">{asset.name}</h1>
            <AssetStatusBadge status={asset.status} />
          </div>
          <p className="mt-1 font-mono text-sm text-brand-700">{asset.assetTag}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          {asset.status === 'Available' && canManage && (
            <button type="button" className="btn-primary" onClick={() => setAssignOpen(true)}>
              <UserPlus className="h-4 w-4" /> Assign
            </button>
          )}
          {asset.status === 'Assigned' && canManage && (
            <button
              type="button"
              className="btn-primary"
              disabled={returnMutation.isPending}
              onClick={() => void returnMutation.mutateAsync({ assetId: asset.id, conditionNotes: null })}
            >
              <RotateCcw className="h-4 w-4" /> Return
            </button>
          )}
          {asset.status !== 'Retired' && (
            <button type="button" className="btn-secondary" onClick={() => setReportOpen(true)}>
              <Wrench className="h-4 w-4" /> Report issue
            </button>
          )}
          {canManage && (
            <button type="button" className="btn-secondary" onClick={() => setEditOpen(true)}>
              <Pencil className="h-4 w-4" /> Edit
            </button>
          )}
          <Link
            to={`/labels?tag=${encodeURIComponent(asset.assetTag)}`}
            className="btn-secondary"
          >
            <Printer className="h-4 w-4" /> Print label
          </Link>
          {canDelete && (
            <button type="button" className="btn-danger" onClick={() => setDeleteOpen(true)}>
              <Trash2 className="h-4 w-4" /> Delete
            </button>
          )}
        </div>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="card p-5 lg:col-span-2">
          <h2 className="text-base font-bold text-slate-900">Asset details</h2>
          <dl className="mt-4 grid gap-x-6 gap-y-4 sm:grid-cols-2">
            {[
              { label: 'Category', value: CATEGORY_LABELS[asset.category] },
              { label: 'Status', value: STATUS_LABELS[asset.status] },
              { label: 'Serial number', value: asset.serialNumber ?? '—' },
              { label: 'Current assignee', value: asset.currentAssigneeName ?? '—' },
              { label: 'Purchase date', value: formatDate(asset.purchaseDate) },
              { label: 'Warranty expiry', value: formatDate(asset.warrantyExpiryDate) },
              { label: 'Assigned on', value: formatDate(asset.assignedDate) },
              { label: 'Added on', value: formatDate(asset.createdAt) }
            ].map((item) => (
              <div key={item.label}>
                <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">{item.label}</dt>
                <dd className="mt-1 text-sm text-slate-800">{item.value}</dd>
              </div>
            ))}
          </dl>
        </div>

        <div className="card flex flex-col items-center justify-center gap-3 p-5">
          <h2 className="self-start text-base font-bold text-slate-900">QR tag</h2>
          {asset.qrCodeBase64 ? (
            <>
              <QRCodeImage base64={asset.qrCodeBase64} tag={asset.assetTag} className="h-44 w-44" />
              <button type="button" className="btn-secondary w-full text-xs" onClick={downloadQr}>
                <Download className="h-3.5 w-3.5" /> Download QR
              </button>
            </>
          ) : (
            <QrCode className="h-12 w-12 text-slate-300" />
          )}
          <p className="text-center text-xs text-slate-500">Scan to open this asset instantly.</p>
        </div>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <div className="card overflow-hidden">
          <div className="flex items-center gap-2 border-b border-slate-200 px-5 py-4">
            <CalendarDays className="h-4 w-4 text-slate-400" />
            <h2 className="text-base font-bold text-slate-900">Assignment history</h2>
          </div>
          {!history?.length ? (
            <EmptyState title="No assignments yet" description="Assignments will appear here once this asset is handed out." />
          ) : (
            <ul className="divide-y divide-slate-100">
              {history.map((item) => (
                <li key={item.id} className="px-5 py-3.5">
                  <div className="flex items-start justify-between gap-3">
                    <p className="text-sm font-medium text-slate-800">{item.assignedToUserName}</p>
                    <span className={`text-xs ${item.returnedDate ? 'text-slate-400' : 'font-semibold text-blue-600'}`}>
                      {item.returnedDate ? 'Returned' : 'Active'}
                    </span>
                  </div>
                  <p className="mt-0.5 text-xs text-slate-500">
                    {formatDateTime(item.assignedDate)} · issued by {item.assignedByUserName}
                  </p>
                  {item.returnedDate && (
                    <p className="mt-0.5 text-xs text-slate-500">Returned {formatDateTime(item.returnedDate)}</p>
                  )}
                  {item.conditionNotes && <p className="mt-1 text-xs text-slate-500">“{item.conditionNotes}”</p>}
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="card overflow-hidden">
          <div className="flex items-center gap-2 border-b border-slate-200 px-5 py-4">
            <Wrench className="h-4 w-4 text-slate-400" />
            <h2 className="text-base font-bold text-slate-900">Maintenance logs</h2>
          </div>
          {!logs?.length ? (
            <EmptyState title="No maintenance logs" description="Issues reported for this asset will appear here." />
          ) : (
            <ul className="divide-y divide-slate-100">
              {logs.map((log) => (
                <li key={log.id} className="px-5 py-3.5">
                  <div className="flex items-start justify-between gap-3">
                    <p className="text-sm text-slate-800">{log.issueDescription}</p>
                    <MaintenanceStatusBadge status={log.status} />
                  </div>
                  <p className="mt-1 text-xs text-slate-500">
                    Reported by {log.reportedByName} · {formatDateTime(log.createdAt)}
                  </p>
                  {log.status !== 'Resolved' && canManage && (
                    <button
                      type="button"
                      className="mt-2 inline-flex items-center gap-1 text-xs font-semibold text-emerald-600 hover:underline"
                      disabled={resolveMutation.isPending}
                      onClick={() => void resolveMutation.mutateAsync(log.id)}
                    >
                      <CheckCircle2 className="h-3.5 w-3.5" /> Mark as resolved
                    </button>
                  )}
                  {log.resolvedAt && (
                    <p className="mt-1 text-xs text-slate-500">Resolved {formatDateTime(log.resolvedAt)}</p>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      <AssetFormModal
        open={editOpen}
        onClose={() => setEditOpen(false)}
        asset={asset}
        loading={updateMutation.isPending}
        onSubmit={async (data) => {
          await updateMutation.mutateAsync({ id: asset.id, data: data as UpdateAssetRequest })
          setEditOpen(false)
        }}
      />

      {canManage && (
        <AssignAssetModal
          open={assignOpen}
          onClose={() => setAssignOpen(false)}
          asset={asset}
          loading={assignMutation.isPending}
          onSubmit={async (employeeId, conditionNotes) => {
            await assignMutation.mutateAsync({
              assetId: asset.id,
              assignedToUserId: employeeId,
              conditionNotes: conditionNotes || null
            }).catch(() => undefined)
            setAssignOpen(false)
          }}
        />
      )}

      <ConfirmDialog
        open={deleteOpen}
        title="Delete asset?"
        message={`This will permanently delete "${asset.name}" and its history.`}
        confirmLabel="Delete asset"
        loading={deleteMutation.isPending}
        onConfirm={async () => {
          await deleteMutation.mutateAsync(asset.id)
          navigate('/assets')
        }}
        onClose={() => setDeleteOpen(false)}
      />

      <Modal open={reportOpen} onClose={() => setReportOpen(false)} title="Report maintenance issue">
        <form onSubmit={handleReport} className="space-y-4">
          <div>
            <label className="label-field" htmlFor="issue">Issue description *</label>
            <textarea
              id="issue"
              className="input-field min-h-28 resize-y"
              value={issue}
              onChange={(event) => setIssue(event.target.value)}
              placeholder="e.g. Battery drains within two hours"
              maxLength={2000}
              required
              autoFocus
            />
          </div>
          <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
            <button type="button" className="btn-secondary" onClick={() => setReportOpen(false)} disabled={reportMutation.isPending}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={reportMutation.isPending}>
              {reportMutation.isPending ? 'Submitting…' : 'Submit report'}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  )
}
