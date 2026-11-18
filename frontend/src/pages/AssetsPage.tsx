import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Download, FileSpreadsheet, Plus, Search } from 'lucide-react'
import { AssetFormModal } from '../components/assets/AssetFormModal'
import { AssignAssetModal } from '../components/assets/AssignAssetModal'
import { AssetTable } from '../components/assets/AssetTable'
import { ConfirmDialog } from '../components/common/ConfirmDialog'
import { EmptyState } from '../components/common/EmptyState'
import { ErrorState } from '../components/common/ErrorState'
import { LoadingSpinner } from '../components/common/LoadingSpinner'
import { Modal } from '../components/common/Modal'
import { QRCodeImage } from '../components/common/QRCodeImage'
import { ASSET_CATEGORIES, ASSET_STATUSES } from '../constants'
import { useAuth } from '../context/AuthContext'
import { useAssets, useCreateAsset, useDeleteAsset, useImportAssets } from '../hooks/useAssets'
import { useAssignAsset } from '../hooks/useAssignments'
import { useDebounce } from '../hooks/useDebounce'
import { storeLabelTags } from '../hooks/useAssetsByTags'
import type {
  Asset,
  AssetCategory,
  AssetStatus,
  CreateAssetRequest,
  ImportAssetsResult,
  UpdateAssetRequest
} from '../types'

export function AssetsPage() {
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState<AssetCategory | ''>('')
  const [status, setStatus] = useState<AssetStatus | ''>('')
  const [formOpen, setFormOpen] = useState(false)
  const [assignAsset, setAssignAsset] = useState<Asset | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<Asset | null>(null)
  const [createdAsset, setCreatedAsset] = useState<Asset | null>(null)
  const [importResult, setImportResult] = useState<ImportAssetsResult | null>(null)
  const [pageNumber, setPageNumber] = useState(1)
  const [sortBy, setSortBy] = useState('created')
  const [descending, setDescending] = useState(true)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const debouncedSearch = useDebounce(search, 300)
  const navigate = useNavigate()
  const { hasRole } = useAuth()

  const filters = {
    search: debouncedSearch || undefined,
    category: category || undefined,
    status: status || undefined,
    pageNumber,
    pageSize: 12,
    sortBy,
    descending
  }
  const { data: assets, isLoading, isError, refetch } = useAssets(filters)
  const createMutation = useCreateAsset()
  const assignMutation = useAssignAsset()
  const deleteMutation = useDeleteAsset()
  const importMutation = useImportAssets()

  // Deleting the last row on the final page would otherwise leave an empty page.
  useEffect(() => {
    if (assets && assets.totalPages > 0 && pageNumber > assets.totalPages) {
      setPageNumber(assets.totalPages)
    }
  }, [assets, pageNumber])

  const canManage = hasRole('ITManager', 'SuperAdmin')
  const canDelete = hasRole('SuperAdmin')

  const handleCreate = async (data: CreateAssetRequest | UpdateAssetRequest) => {
    const asset = await createMutation.mutateAsync(data as CreateAssetRequest)
    setFormOpen(false)
    setCreatedAsset(asset)
  }

  const handleAssign = async (employeeId: string, conditionNotes: string) => {
    if (!assignAsset) return
    await assignMutation.mutateAsync({
      assetId: assignAsset.id,
      assignedToUserId: employeeId,
      conditionNotes: conditionNotes || null
    }).catch(() => undefined)
    setAssignAsset(null)
  }

  const handleImport = async (file: File) => {
    const result = await importMutation.mutateAsync(file)
    setImportResult(result)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    await deleteMutation.mutateAsync(deleteTarget.id).catch(() => undefined)
    setDeleteTarget(null)
  }

  const hasFilters = Boolean(search || category || status)

  const clearFilters = () => {
    setSearch('')
    setCategory('')
    setStatus('')
    setPageNumber(1)
  }

  const downloadQr = () => {
    if (!createdAsset?.qrCodeBase64) return
    const link = document.createElement('a')
    link.href = `data:image/png;base64,${createdAsset.qrCodeBase64}`
    link.download = `${createdAsset.assetTag}.png`
    link.click()
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 sm:text-3xl">Assets</h1>
          <p className="mt-1 text-sm text-slate-500">Track every tagged item in your organisation.</p>
        </div>
        {canManage && (
          <div className="flex flex-wrap gap-2">
            <button type="button" className="btn-secondary" onClick={() => fileInputRef.current?.click()}>
              <FileSpreadsheet className="h-4 w-4" /> Import Excel
            </button>
            <input
              ref={fileInputRef}
              type="file"
              accept=".xlsx,.xlsm"
              className="hidden"
              onChange={(event) => {
                const file = event.target.files?.[0]
                if (file) void handleImport(file)
              }}
              aria-label="Import assets from Excel"
            />
            <button type="button" className="btn-primary self-start" onClick={() => setFormOpen(true)}>
              <Plus className="h-4 w-4" /> Add asset
            </button>
          </div>
        )}
      </div>

      <div className="card overflow-hidden">
        <div className="flex flex-col gap-3 border-b border-slate-200 p-4 sm:flex-row sm:items-center">
          <div className="relative flex-1">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              className="input-field pl-9"
              placeholder="Search name, tag or serial number…"
              value={search}
              onChange={(event) => { setSearch(event.target.value); setPageNumber(1) }}
              aria-label="Search assets"
            />
          </div>
          <div className="flex flex-wrap gap-2">
            <select
              className="input-field sm:w-40"
              value={category}
              onChange={(event) => { setCategory(event.target.value as AssetCategory | ''); setPageNumber(1) }}
              aria-label="Filter by category"
            >
              <option value="">All categories</option>
              {ASSET_CATEGORIES.map((item) => <option key={item} value={item}>{item}</option>)}
            </select>
            <select
              className="input-field sm:w-44"
              value={status}
              onChange={(event) => { setStatus(event.target.value as AssetStatus | ''); setPageNumber(1) }}
              aria-label="Filter by status"
            >
              <option value="">All statuses</option>
              {ASSET_STATUSES.map((item) => <option key={item} value={item}>{item}</option>)}
            </select>
            <select
              className="input-field sm:w-40"
              value={`${sortBy}:${descending ? 'desc' : 'asc'}`}
              onChange={(event) => {
                const [field, direction] = event.target.value.split(':')
                setSortBy(field)
                setDescending(direction === 'desc')
                setPageNumber(1)
              }}
              aria-label="Sort assets"
            >
              <option value="created:desc">Newest first</option>
              <option value="created:asc">Oldest first</option>
              <option value="name:asc">Name A–Z</option>
              <option value="name:desc">Name Z–A</option>
              <option value="purchase:desc">Recently purchased</option>
            </select>
          </div>
        </div>

        {isLoading || (assets && pageNumber > assets.totalPages && assets.totalPages > 0) ? (
          <div className="flex min-h-64 items-center justify-center"><LoadingSpinner label="Loading assets…" /></div>
        ) : isError ? (
          <div className="p-4"><ErrorState message="Could not load assets." onRetry={() => void refetch()} /></div>
        ) : !assets?.items.length ? (
          <EmptyState
            title={hasFilters ? 'No matching assets' : 'No assets yet'}
            description={hasFilters ? 'Try changing or clearing your filters.' : 'Add your first asset to start tracking it with a QR code.'}
            action={hasFilters
              ? <button type="button" className="btn-secondary" onClick={clearFilters}>Clear filters</button>
              : canManage
                ? <button type="button" className="btn-primary" onClick={() => setFormOpen(true)}><Plus className="h-4 w-4" /> Add asset</button>
                : undefined}
          />
        ) : (
          <>
            <div className="flex items-center justify-between border-b border-slate-100 bg-slate-50/60 px-5 py-2.5 text-xs text-slate-500">
              <span>{assets.totalCount} asset{assets.totalCount === 1 ? '' : 's'}</span>
              {hasFilters && <button type="button" className="font-medium text-brand-600 hover:underline" onClick={clearFilters}>Clear filters</button>}
            </div>
            <AssetTable
              assets={assets.items}
              canManage={canManage}
              canDelete={canDelete}
              onView={(id) => navigate(`/assets/${id}`)}
              onAssign={setAssignAsset}
              onDelete={setDeleteTarget}
            />
            {assets.totalPages > 1 && (
              <div className="flex items-center justify-between gap-3 border-t border-slate-200 px-5 py-3 text-xs text-slate-500">
                <span>
                  Page {assets.pageNumber} of {assets.totalPages}
                </span>
                <div className="flex gap-2">
                  <button
                    type="button"
                    className="btn-secondary px-3 py-1.5 text-xs"
                    disabled={assets.pageNumber <= 1}
                    onClick={() => setPageNumber((page) => page - 1)}
                  >
                    Previous
                  </button>
                  <button
                    type="button"
                    className="btn-secondary px-3 py-1.5 text-xs"
                    disabled={assets.pageNumber >= assets.totalPages}
                    onClick={() => setPageNumber((page) => page + 1)}
                  >
                    Next
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      <AssetFormModal
        open={formOpen}
        onClose={() => setFormOpen(false)}
        loading={createMutation.isPending}
        onSubmit={handleCreate}
      />

      <AssignAssetModal
        open={Boolean(assignAsset)}
        onClose={() => setAssignAsset(null)}
        asset={assignAsset}
        loading={assignMutation.isPending}
        onSubmit={handleAssign}
      />

      <ConfirmDialog
        open={Boolean(deleteTarget)}
        title="Delete asset?"
        message={`This will permanently delete "${deleteTarget?.name}" and its assignment/maintenance history. This action cannot be undone.`}
        confirmLabel="Delete asset"
        loading={deleteMutation.isPending}
        onConfirm={handleDelete}
        onClose={() => setDeleteTarget(null)}
      />

      <Modal
        open={Boolean(createdAsset)}
        onClose={() => setCreatedAsset(null)}
        title="Asset created"
        description="Print or save this QR code and attach it to the asset."
      >
        {createdAsset && (
          <div className="flex flex-col items-center gap-4 text-center">
            {createdAsset.qrCodeBase64 && (
              <QRCodeImage base64={createdAsset.qrCodeBase64} tag={createdAsset.assetTag} className="h-52 w-52" />
            )}
            <div>
              <p className="text-sm font-semibold text-slate-800">{createdAsset.name}</p>
              <p className="mt-1 font-mono text-sm text-brand-700">{createdAsset.assetTag}</p>
            </div>
            <button type="button" className="btn-primary w-full sm:w-auto" onClick={downloadQr}>
              <Download className="h-4 w-4" /> Download QR code
            </button>
          </div>
        )}
      </Modal>

      <Modal
        open={Boolean(importResult)}
        onClose={() => setImportResult(null)}
        title="Import complete"
        description="Review the results below. Invalid rows were skipped, nothing else was changed."
        size="lg"
      >
        {importResult && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="rounded-lg border border-emerald-100 bg-emerald-50 p-4">
                <p className="text-2xl font-bold text-emerald-700">{importResult.created}</p>
                <p className="text-xs font-medium text-emerald-700">Assets created</p>
              </div>
              <div className="rounded-lg border border-amber-100 bg-amber-50 p-4">
                <p className="text-2xl font-bold text-amber-700">{importResult.skipped}</p>
                <p className="text-xs font-medium text-amber-700">Rows skipped</p>
              </div>
            </div>

            {importResult.created > 500 && (
              <p className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-700">
                Label sheets support up to 500 tags at a time — the first 500 will be printed.
              </p>
            )}

            {importResult.errors.length > 0 && (
              <div className="max-h-44 overflow-y-auto rounded-lg border border-red-100 bg-red-50 p-3">
                <p className="mb-1.5 text-xs font-semibold text-red-700">Row errors</p>
                <ul className="space-y-1 text-xs text-red-700">
                  {importResult.errors.map((error) => <li key={error}>• {error}</li>)}
                </ul>
              </div>
            )}

            <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button type="button" className="btn-secondary" onClick={() => setImportResult(null)}>Close</button>
              {importResult.created > 0 && (
                <button
                  type="button"
                  className="btn-primary"
                  onClick={() => {
                    // The batch endpoint accepts at most 500 tags.
                    storeLabelTags(importResult.assets.slice(0, 500).map((asset) => asset.assetTag))
                    setImportResult(null)
                    navigate('/labels')
                  }}
                >
                  <Download className="h-4 w-4" /> Print QR labels
                </button>
              )}
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}
