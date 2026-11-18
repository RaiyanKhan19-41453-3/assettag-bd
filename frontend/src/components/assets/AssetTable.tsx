import { Eye, Trash2, UserPlus } from 'lucide-react'
import { AssetStatusBadge } from '../common/StatusBadge'
import { CATEGORY_LABELS } from '../../constants'
import type { Asset } from '../../types'

interface AssetTableProps {
  assets: Asset[]
  canManage: boolean
  canDelete: boolean
  onView: (id: string) => void
  onAssign: (asset: Asset) => void
  onDelete: (asset: Asset) => void
}

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

export function AssetTable({
  assets,
  canManage,
  canDelete,
  onView,
  onAssign,
  onDelete
}: AssetTableProps) {
  return (
    <>
      {/* Desktop table */}
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-slate-200 bg-slate-50/80">
            <tr className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              <th className="px-5 py-3">Asset</th>
              <th className="px-5 py-3">Category</th>
              <th className="px-5 py-3">Status</th>
              <th className="px-5 py-3">Assigned to</th>
              <th className="px-5 py-3">Purchased</th>
              <th className="px-5 py-3 text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {assets.map((asset) => (
              <tr key={asset.id} className="transition hover:bg-slate-50/70">
                <td className="px-5 py-3.5">
                  <button
                    type="button"
                    onClick={() => onView(asset.id)}
                    className="text-left font-semibold text-brand-700 hover:underline"
                  >
                    {asset.name}
                  </button>
                  <p className="mt-0.5 font-mono text-xs text-slate-500">{asset.assetTag}</p>
                </td>
                <td className="px-5 py-3.5 text-slate-600">{CATEGORY_LABELS[asset.category]}</td>
                <td className="px-5 py-3.5"><AssetStatusBadge status={asset.status} /></td>
                <td className="px-5 py-3.5 text-slate-600">{asset.currentAssigneeName ?? '—'}</td>
                <td className="px-5 py-3.5 text-slate-600">{formatDate(asset.purchaseDate)}</td>
                <td className="px-5 py-3.5">
                  <div className="flex justify-end gap-1">
                    <button
                      type="button"
                      onClick={() => onView(asset.id)}
                      className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-brand-600"
                      title="View details"
                      aria-label={`View ${asset.name}`}
                    >
                      <Eye className="h-4 w-4" />
                    </button>
                    {canManage && asset.status === 'Available' && (
                      <button
                        type="button"
                        onClick={() => onAssign(asset)}
                        className="rounded-lg p-2 text-slate-500 hover:bg-blue-50 hover:text-blue-600"
                        title="Assign asset"
                        aria-label={`Assign ${asset.name}`}
                      >
                        <UserPlus className="h-4 w-4" />
                      </button>
                    )}
                    {canDelete && (
                      <button
                        type="button"
                        onClick={() => onDelete(asset)}
                        className="rounded-lg p-2 text-slate-500 hover:bg-red-50 hover:text-red-600"
                        title="Delete asset"
                        aria-label={`Delete ${asset.name}`}
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile cards */}
      <div className="divide-y divide-slate-100 md:hidden">
        {assets.map((asset) => (
          <div key={asset.id} className="p-4">
            <div className="flex items-start justify-between gap-3">
              <button
                type="button"
                onClick={() => onView(asset.id)}
                className="text-left font-semibold text-slate-900"
              >
                {asset.name}
              </button>
              <AssetStatusBadge status={asset.status} />
            </div>
            <p className="mt-1 font-mono text-xs text-slate-500">{asset.assetTag}</p>
            <p className="mt-2 text-xs text-slate-500">
              {CATEGORY_LABELS[asset.category]} · {formatDate(asset.purchaseDate)}
            </p>
            {asset.currentAssigneeName && (
              <p className="mt-1 text-xs text-slate-500">Assigned to {asset.currentAssigneeName}</p>
            )}
            <div className="mt-3 flex gap-2">
              <button type="button" onClick={() => onView(asset.id)} className="btn-secondary flex-1 py-2 text-xs">
                <Eye className="h-3.5 w-3.5" /> View
              </button>
              {canManage && asset.status === 'Available' && (
                <button type="button" onClick={() => onAssign(asset)} className="btn-primary flex-1 py-2 text-xs">
                  <UserPlus className="h-3.5 w-3.5" /> Assign
                </button>
              )}
              {canDelete && (
                <button type="button" onClick={() => onDelete(asset)} className="btn-danger px-3 py-2 text-xs">
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </>
  )
}
