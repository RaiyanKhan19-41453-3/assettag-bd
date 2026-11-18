import { Link, useSearchParams } from 'react-router-dom'
import { ArrowLeft, Printer } from 'lucide-react'
import { LoadingSpinner } from '../components/common/LoadingSpinner'
import { QRCodeImage } from '../components/common/QRCodeImage'
import { readStoredLabelTags, useAssetsByTags } from '../hooks/useAssetsByTags'

export function LabelsPage() {
  const [params] = useSearchParams()

  // Large imports store tags in sessionStorage to avoid a huge URL. A single
  // tag from the asset detail page is passed via the query string.
  const queryTag = params.get('tag')
  const stored = readStoredLabelTags()
  // An explicit single tag (from asset detail) wins over a stale bulk selection.
  const tags = queryTag ? [queryTag] : stored

  const { data: assets, isLoading, isError, refetch } = useAssetsByTags(tags)

  return (
    <div className="space-y-5">
      <div className="no-print flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <Link to="/assets" className="inline-flex items-center gap-1.5 text-sm text-brand-600 hover:underline">
            <ArrowLeft className="h-4 w-4" /> Back to assets
          </Link>
          <h1 className="mt-2 text-2xl font-bold text-slate-900">Print QR labels</h1>
          <p className="mt-1 text-sm text-slate-500">
            {assets?.length ?? 0} label{assets?.length === 1 ? '' : 's'} ready for an A4 sheet.
          </p>
        </div>
        <button type="button" className="btn-primary self-start" onClick={() => window.print()} disabled={!assets?.length}>
          <Printer className="h-4 w-4" /> Print labels
        </button>
      </div>

      {tags.length === 0 && (
        <div className="card p-8 text-center text-sm text-slate-500">
          No assets selected. Select assets in the asset list first.
        </div>
      )}

      {isLoading && (
        <div className="card flex min-h-48 items-center justify-center">
          <LoadingSpinner label="Preparing labels…" />
        </div>
      )}

      {isError && (
        <div className="card p-8 text-center text-sm text-red-600">
          Could not load labels.{' '}
          <button type="button" className="font-semibold underline" onClick={() => void refetch()}>Retry</button>
        </div>
      )}

      {!isLoading && !isError && assets?.length === 0 && tags.length > 0 && (
        <div className="card p-8 text-center text-sm text-slate-500">No assets matched the selected tags.</div>
      )}

      {assets && assets.length > 0 && (
        <div id="print-area" className="label-sheet">
          {assets.map((asset) => (
            <div key={asset.id} className="asset-label">
              {asset.qrCodeBase64 && (
                <QRCodeImage base64={asset.qrCodeBase64} tag={asset.assetTag} className="label-qr" />
              )}
              <p className="label-tag">{asset.assetTag}</p>
              <p className="label-name">{asset.name}</p>
              <p className="label-category">{asset.category}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
