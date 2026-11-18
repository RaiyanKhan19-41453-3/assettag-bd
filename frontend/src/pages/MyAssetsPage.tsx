import { Link } from 'react-router-dom'
import { ArrowUpRight, CalendarDays, Laptop, ShieldCheck } from 'lucide-react'
import { EmptyState } from '../components/common/EmptyState'
import { ErrorState } from '../components/common/ErrorState'
import { LoadingSpinner } from '../components/common/LoadingSpinner'
import { QRCodeImage } from '../components/common/QRCodeImage'
import { CATEGORY_LABELS } from '../constants'
import { useAuth } from '../context/AuthContext'
import { useMyAssets } from '../hooks/useMyAssets'

function formatDate(value?: string | null): string {
  return value ? new Date(value).toLocaleDateString() : '—'
}

export function MyAssetsPage() {
  const { user } = useAuth()
  const { data: assets, isLoading, isError, refetch } = useMyAssets()

  if (isLoading) {
    return <div className="card flex min-h-64 items-center justify-center"><LoadingSpinner label="Loading your assets…" /></div>
  }

  if (isError) {
    return <ErrorState message="Could not load your assets." onRetry={() => void refetch()} />
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 sm:text-3xl">My assets</h1>
        <p className="mt-1 text-sm text-slate-500">
          Items currently assigned to you{user ? `, ${user.fullName}` : ''}. Please look after them.
        </p>
      </div>

      {!assets?.length ? (
        <div className="card">
          <EmptyState
            title="No assets assigned to you"
            description="When the IT team assigns you a laptop, phone or other equipment, it will appear here."
          />
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {assets.map((asset) => (
            <div key={asset.id} className="card overflow-hidden">
              <div className="flex items-start gap-4 p-5">
                {asset.qrCodeBase64 ? (
                  <QRCodeImage base64={asset.qrCodeBase64} tag={asset.assetTag} className="h-20 w-20" />
                ) : (
                  <div className="flex h-20 w-20 items-center justify-center rounded-lg bg-slate-100 text-slate-400">
                    <Laptop className="h-8 w-8" />
                  </div>
                )}
                <div className="min-w-0 flex-1">
                  <p className="truncate font-semibold text-slate-900">{asset.name}</p>
                  <p className="mt-1 font-mono text-xs text-brand-700">{asset.assetTag}</p>
                  <p className="mt-1 text-xs text-slate-500">{CATEGORY_LABELS[asset.category]}</p>
                </div>
              </div>

              <dl className="grid grid-cols-2 gap-3 border-t border-slate-100 bg-slate-50/60 px-5 py-3 text-xs">
                <div>
                  <dt className="flex items-center gap-1 text-slate-500"><CalendarDays className="h-3 w-3" /> Assigned</dt>
                  <dd className="mt-0.5 font-medium text-slate-800">{formatDate(asset.assignedDate)}</dd>
                </div>
                <div>
                  <dt className="flex items-center gap-1 text-slate-500"><ShieldCheck className="h-3 w-3" /> Warranty</dt>
                  <dd className="mt-0.5 font-medium text-slate-800">{formatDate(asset.warrantyExpiryDate)}</dd>
                </div>
              </dl>

              <div className="px-5 py-3">
                <Link
                  to={`/assets/${asset.id}`}
                  className="inline-flex items-center gap-1 text-xs font-semibold text-brand-600 hover:underline"
                >
                  View details <ArrowUpRight className="h-3.5 w-3.5" />
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
