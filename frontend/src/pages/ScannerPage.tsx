import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Html5Qrcode } from 'html5-qrcode'
import {
  Camera,
  CameraOff,
  Keyboard,
  PackageSearch,
  RotateCcw,
  Search,
  UserPlus
} from 'lucide-react'
import { AssignAssetModal } from '../components/assets/AssignAssetModal'
import { Modal } from '../components/common/Modal'
import { QRCodeImage } from '../components/common/QRCodeImage'
import { AssetStatusBadge } from '../components/common/StatusBadge'
import { ApiError } from '../services/api'
import { useAssetByTag } from '../hooks/useAssets'
import { useAssignAsset, useReturnAsset } from '../hooks/useAssignments'

const READER_ID = 'qr-reader'

export function ScannerPage() {
  const [tag, setTag] = useState<string | null>(null)
  const [manualTag, setManualTag] = useState('')
  const [scanning, setScanning] = useState(false)
  const [cameraError, setCameraError] = useState('')
  const [assignOpen, setAssignOpen] = useState(false)
  const [returnNotes, setReturnNotes] = useState('')
  const [returnOpen, setReturnOpen] = useState(false)

  const scannerRef = useRef<Html5Qrcode | null>(null)
  const lastScannedRef = useRef<string | null>(null)
  const mountedRef = useRef(true)

  const { data: asset, isLoading, isError, error, refetch } = useAssetByTag(tag)
  const assignMutation = useAssignAsset()
  const returnMutation = useReturnAsset()

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
      const scanner = scannerRef.current
      if (scanner) {
        void scanner.stop().then(() => scanner.clear()).catch(() => undefined)
        scannerRef.current = null
      }
    }
  }, [])

  const stopScanner = async () => {
    const scanner = scannerRef.current
    scannerRef.current = null
    if (scanner) {
      try {
        await scanner.stop()
        scanner.clear()
      } catch {
        // Camera may already be released (e.g. browser revoked permission).
      }
    }
    if (mountedRef.current) setScanning(false)
  }

  const startScanner = async () => {
    if (scannerRef.current) return
    setCameraError('')
    lastScannedRef.current = null

    try {
      const scanner = new Html5Qrcode(READER_ID)
      scannerRef.current = scanner

      await scanner.start(
        { facingMode: 'environment' },
        { fps: 10, qrbox: { width: 260, height: 260 }, aspectRatio: 1.2 },
        (decodedText) => {
          if (lastScannedRef.current === decodedText) return
          lastScannedRef.current = decodedText
          setManualTag('')
          setTag(decodedText.trim().toUpperCase())
          void stopScanner()
        },
        () => undefined
      )

      // If the user navigated away while start() was pending, release the camera.
      if (!mountedRef.current) {
        await scanner.stop().catch(() => undefined)
        scanner.clear()
        scannerRef.current = null
        return
      }

      setScanning(true)
    } catch (err) {
      scannerRef.current = null
      if (mountedRef.current) {
        setScanning(false)
        setCameraError(
          err instanceof Error
            ? `Could not start the camera: ${err.message}. Check camera permission, or enter the asset tag manually.`
            : 'Could not start the camera. Please enter the asset tag manually.'
        )
      }
    }
  }

  const handleManualLookup = (event: FormEvent) => {
    event.preventDefault()
    const value = manualTag.trim()
    if (!value) return
    lastScannedRef.current = value.toUpperCase()
    setTag(value.toUpperCase())
    void stopScanner()
  }

  const reset = () => {
    setTag(null)
    setManualTag('')
    setCameraError('')
    lastScannedRef.current = null
  }

  const notFound = isError && error instanceof ApiError && error.status === 404

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 sm:text-3xl">Scan asset QR</h1>
        <p className="mt-1 text-sm text-slate-500">Point your camera at an asset tag to look it up instantly.</p>
      </div>

      <div className="grid gap-5 lg:grid-cols-2">
        <div className="space-y-4">
          <div className="card overflow-hidden">
            <div className="relative min-h-[300px] bg-slate-900 sm:min-h-[360px]">
              <div id={READER_ID} className="h-full w-full min-h-[300px] sm:min-h-[360px]" />

              {!scanning && (
                <div className="absolute inset-0 flex flex-col items-center justify-center gap-4 px-6 text-center text-white">
                  <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-white/10">
                    <Camera className="h-8 w-8" />
                  </div>
                  <div>
                    <p className="font-semibold">Ready to scan</p>
                    <p className="mt-1 text-sm text-slate-300">Allow camera access when prompted.</p>
                  </div>
                  <button type="button" className="btn-primary" onClick={() => void startScanner()}>
                    <Camera className="h-4 w-4" /> Start camera
                  </button>
                </div>
              )}
            </div>

            {scanning && (
              <div className="flex items-center justify-between gap-3 border-t border-slate-200 px-4 py-3">
                <p className="flex items-center gap-2 text-sm font-medium text-emerald-600">
                  <span className="h-2 w-2 animate-pulse rounded-full bg-emerald-500" />
                  Camera active — scanning…
                </p>
                <button type="button" className="btn-secondary py-2 text-xs" onClick={() => void stopScanner()}>
                  <CameraOff className="h-3.5 w-3.5" /> Stop
                </button>
              </div>
            )}
          </div>

          {cameraError && (
            <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800" role="alert">
              {cameraError}
            </div>
          )}

          <div className="card p-4">
            <form onSubmit={handleManualLookup} className="space-y-3">
              <label className="label-field flex items-center gap-2" htmlFor="manual-tag">
                <Keyboard className="h-4 w-4 text-slate-400" />
                Or enter the asset tag manually
              </label>
              <div className="flex gap-2">
                <input
                  id="manual-tag"
                  className="input-field font-mono uppercase"
                  placeholder="ATBD-LAP-2026-000001"
                  value={manualTag}
                  onChange={(event) => setManualTag(event.target.value)}
                />
                <button type="submit" className="btn-primary shrink-0" disabled={!manualTag.trim()}>
                  <Search className="h-4 w-4" />
                  <span className="hidden sm:inline">Look up</span>
                </button>
              </div>
            </form>
          </div>
        </div>

        <div>
          {!tag && (
            <div className="card flex min-h-[300px] flex-col items-center justify-center gap-3 p-6 text-center">
              <PackageSearch className="h-12 w-12 text-slate-300" />
              <p className="font-semibold text-slate-700">No asset scanned yet</p>
              <p className="max-w-xs text-sm text-slate-500">
                Scanned asset details and quick actions will appear here.
              </p>
            </div>
          )}

          {tag && isLoading && (
            <div className="card flex min-h-[300px] items-center justify-center">
              <div className="text-center">
                <div className="mx-auto h-9 w-9 animate-spin rounded-full border-4 border-brand-100 border-t-brand-600" />
                <p className="mt-3 text-sm text-slate-500">Looking up {tag}…</p>
              </div>
            </div>
          )}

          {tag && notFound && (
            <div className="card flex min-h-[300px] flex-col items-center justify-center gap-3 p-6 text-center">
              <PackageSearch className="h-12 w-12 text-red-300" />
              <p className="font-semibold text-slate-800">Asset not found</p>
              <p className="max-w-xs text-sm text-slate-500">
                No asset matches tag <span className="font-mono text-brand-700">{tag}</span>.
              </p>
              <button type="button" className="btn-secondary mt-2" onClick={reset}>
                <RotateCcw className="h-4 w-4" /> Try another tag
              </button>
            </div>
          )}

          {tag && isError && !notFound && (
            <div className="card flex min-h-[300px] flex-col items-center justify-center gap-3 p-6 text-center">
              <p className="font-semibold text-slate-800">Lookup failed</p>
              <button type="button" className="btn-secondary" onClick={() => void refetch()}>Retry</button>
            </div>
          )}

          {asset && (
            <div className="card overflow-hidden">
              <div className="flex items-start justify-between gap-3 border-b border-slate-200 p-5">
                <div>
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="text-lg font-bold text-slate-900">{asset.name}</h2>
                    <AssetStatusBadge status={asset.status} />
                  </div>
                  <p className="mt-1 font-mono text-sm text-brand-700">{asset.assetTag}</p>
                </div>
                {asset.qrCodeBase64 && (
                  <QRCodeImage base64={asset.qrCodeBase64} tag={asset.assetTag} className="h-20 w-20" />
                )}
              </div>

              <dl className="grid grid-cols-2 gap-4 p-5 text-sm">
                <div>
                  <dt className="text-xs text-slate-500">Category</dt>
                  <dd className="mt-0.5 font-medium text-slate-800">{asset.category}</dd>
                </div>
                <div>
                  <dt className="text-xs text-slate-500">Serial number</dt>
                  <dd className="mt-0.5 font-medium text-slate-800">{asset.serialNumber ?? '—'}</dd>
                </div>
                <div>
                  <dt className="text-xs text-slate-500">Assigned to</dt>
                  <dd className="mt-0.5 font-medium text-slate-800">{asset.currentAssigneeName ?? '—'}</dd>
                </div>
                <div>
                  <dt className="text-xs text-slate-500">Warranty</dt>
                  <dd className="mt-0.5 font-medium text-slate-800">
                    {asset.warrantyExpiryDate ? new Date(asset.warrantyExpiryDate).toLocaleDateString() : '—'}
                  </dd>
                </div>
              </dl>

              <div className="flex flex-col gap-2 border-t border-slate-200 p-5 sm:flex-row">
                {asset.status === 'Available' && (
                  <button type="button" className="btn-primary flex-1" onClick={() => setAssignOpen(true)}>
                    <UserPlus className="h-4 w-4" /> Assign asset
                  </button>
                )}
                {asset.status === 'Assigned' && (
                  <button type="button" className="btn-primary flex-1" onClick={() => setReturnOpen(true)}>
                    <RotateCcw className="h-4 w-4" /> Return asset
                  </button>
                )}
                {asset.status === 'UnderMaintenance' && (
                  <p className="flex-1 rounded-lg bg-amber-50 px-3 py-2.5 text-sm text-amber-700">
                    This asset is under maintenance.
                  </p>
                )}
                {asset.status === 'Retired' && (
                  <p className="flex-1 rounded-lg bg-slate-100 px-3 py-2.5 text-sm text-slate-600">
                    This asset has been retired.
                  </p>
                )}
                <button type="button" className="btn-secondary" onClick={reset}>
                  Scan another
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      <AssignAssetModal
        open={assignOpen}
        onClose={() => setAssignOpen(false)}
        asset={asset ?? undefined}
        tag={tag}
        loading={assignMutation.isPending}
        onSubmit={async (employeeId, conditionNotes) => {
          if (!asset) return
          await assignMutation.mutateAsync({
            assetId: asset.id,
            assignedToUserId: employeeId,
            conditionNotes: conditionNotes || null
          })
          setAssignOpen(false)
        }}
      />

      <Modal
        open={returnOpen}
        onClose={() => setReturnOpen(false)}
        title="Return asset"
        description={asset ? `${asset.name} · ${asset.assetTag}` : undefined}
      >
        <form
          onSubmit={async (event) => {
            event.preventDefault()
            if (!asset) return
            await returnMutation.mutateAsync({
              assetId: asset.id,
              conditionNotes: returnNotes.trim() || null
            })
            setReturnOpen(false)
            setReturnNotes('')
          }}
          className="space-y-4"
        >
          <div>
            <label className="label-field" htmlFor="return-notes">Condition notes (optional)</label>
            <textarea
              id="return-notes"
              className="input-field min-h-24 resize-y"
              value={returnNotes}
              onChange={(event) => setReturnNotes(event.target.value)}
              placeholder="e.g. Minor scratch on lid"
              maxLength={1000}
            />
          </div>
          <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
            <button type="button" className="btn-secondary" onClick={() => setReturnOpen(false)} disabled={returnMutation.isPending}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={returnMutation.isPending}>
              {returnMutation.isPending ? 'Returning…' : 'Confirm return'}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  )
}
