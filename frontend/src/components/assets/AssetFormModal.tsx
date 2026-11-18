import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Modal } from '../common/Modal'
import { ASSET_CATEGORIES, ASSET_STATUSES, toDateInputValue } from '../../constants'
import type { Asset, AssetCategory, AssetStatus, CreateAssetRequest, UpdateAssetRequest } from '../../types'

interface AssetFormModalProps {
  open: boolean
  onClose: () => void
  asset?: Asset | null
  loading?: boolean
  onSubmit: (data: CreateAssetRequest | UpdateAssetRequest) => Promise<void>
}

export function AssetFormModal({ open, onClose, asset, loading = false, onSubmit }: AssetFormModalProps) {
  const [assetTag, setAssetTag] = useState('')
  const [name, setName] = useState('')
  const [category, setCategory] = useState<AssetCategory>('Laptop')
  const [serialNumber, setSerialNumber] = useState('')
  const [purchaseDate, setPurchaseDate] = useState('')
  const [warrantyExpiryDate, setWarrantyExpiryDate] = useState('')
  const [status, setStatus] = useState<AssetStatus>('Available')

  const isEdit = Boolean(asset)

  useEffect(() => {
    if (!open) return
    setAssetTag(asset?.assetTag ?? '')
    setName(asset?.name ?? '')
    setCategory(asset?.category ?? 'Laptop')
    setSerialNumber(asset?.serialNumber ?? '')
    setPurchaseDate(asset ? toDateInputValue(asset.purchaseDate) : new Date().toISOString().slice(0, 10))
    setWarrantyExpiryDate(toDateInputValue(asset?.warrantyExpiryDate))
    setStatus(asset?.status ?? 'Available')
  }, [open, asset])

  const toUtcIso = (value: string, label: string): string => {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value) || Number.isNaN(Date.parse(`${value}T00:00:00Z`))) {
      throw new Error(`${label} is not a valid date.`)
    }
    return new Date(`${value}T00:00:00Z`).toISOString()
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()

    const payload = {
      assetTag: assetTag.trim() || null,
      name: name.trim(),
      category,
      serialNumber: serialNumber.trim() || null,
      purchaseDate: toUtcIso(purchaseDate, 'Purchase date'),
      warrantyExpiryDate: warrantyExpiryDate ? toUtcIso(warrantyExpiryDate, 'Warranty expiry') : null
    }

    const data: CreateAssetRequest | UpdateAssetRequest = isEdit
      ? { ...payload, status }
      : payload

    await onSubmit(data)
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit asset' : 'Add new asset'}
      description={isEdit ? 'Update the details of this asset.' : 'A unique tag and QR code are generated automatically.'}
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="label-field" htmlFor="asset-name">Asset name *</label>
          <input
            id="asset-name"
            className="input-field"
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="e.g. Dell Latitude 5440"
            required
            maxLength={200}
            autoFocus
          />
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="label-field" htmlFor="asset-category">Category *</label>
            <select
              id="asset-category"
              className="input-field"
              value={category}
              onChange={(event) => setCategory(event.target.value as AssetCategory)}
            >
              {ASSET_CATEGORIES.map((item) => (
                <option key={item} value={item}>{item}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="label-field" htmlFor="asset-status">Status</label>
            <select
              id="asset-status"
              className="input-field disabled:bg-slate-50 disabled:text-slate-400"
              value={status}
              onChange={(event) => setStatus(event.target.value as AssetStatus)}
              disabled={!isEdit}
            >
              {ASSET_STATUSES.map((item) => (
                <option key={item} value={item}>{item}</option>
              ))}
            </select>
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="label-field" htmlFor="asset-serial">Serial number</label>
            <input
              id="asset-serial"
              className="input-field"
              value={serialNumber}
              onChange={(event) => setSerialNumber(event.target.value)}
              placeholder="Optional"
              maxLength={100}
            />
          </div>

          <div>
            <label className="label-field" htmlFor="asset-tag">Asset tag</label>
            <input
              id="asset-tag"
              className="input-field disabled:bg-slate-50 disabled:text-slate-400"
              value={assetTag}
              onChange={(event) => setAssetTag(event.target.value)}
              placeholder="Auto-generated"
              maxLength={100}
              disabled={isEdit}
            />
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="label-field" htmlFor="asset-purchase">Purchase date *</label>
            <input
              id="asset-purchase"
              type="date"
              className="input-field"
              value={purchaseDate}
              onChange={(event) => setPurchaseDate(event.target.value)}
              required
            />
          </div>

          <div>
            <label className="label-field" htmlFor="asset-warranty">Warranty expiry</label>
            <input
              id="asset-warranty"
              type="date"
              className="input-field"
              value={warrantyExpiryDate}
              onChange={(event) => setWarrantyExpiryDate(event.target.value)}
            />
          </div>
        </div>

        <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
          <button type="button" className="btn-secondary" onClick={onClose} disabled={loading}>
            Cancel
          </button>
          <button type="submit" className="btn-primary" disabled={loading}>
            {loading ? 'Saving…' : isEdit ? 'Save changes' : 'Create asset'}
          </button>
        </div>
      </form>
    </Modal>
  )
}
