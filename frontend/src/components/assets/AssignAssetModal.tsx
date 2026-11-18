import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Modal } from '../common/Modal'
import { ErrorState } from '../common/ErrorState'
import { LoadingSpinner } from '../common/LoadingSpinner'
import { useUsers } from '../../hooks/useUsers'
import type { Asset } from '../../types'

interface AssignAssetModalProps {
  open: boolean
  onClose: () => void
  asset?: Asset | null
  tag?: string | null
  loading?: boolean
  onSubmit: (employeeId: string, conditionNotes: string) => Promise<void>
}

export function AssignAssetModal({
  open,
  onClose,
  asset,
  tag,
  loading = false,
  onSubmit
}: AssignAssetModalProps) {
  const [employeeId, setEmployeeId] = useState('')
  const [conditionNotes, setConditionNotes] = useState('')
  const { data: employees, isLoading, isError, refetch } = useUsers('Employee')

  useEffect(() => {
    if (open) {
      setEmployeeId('')
      setConditionNotes('')
    }
  }, [open])

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!employeeId) return
    await onSubmit(employeeId, conditionNotes.trim())
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Assign asset"
      description={`${asset?.name ?? ''} ${asset?.assetTag || tag ? `· ${asset?.assetTag ?? tag}` : ''}`.trim() || 'Choose an employee to hand this asset to.'}
    >
      {isLoading ? (
        <div className="py-8"><LoadingSpinner label="Loading employees…" /></div>
      ) : isError ? (
        <ErrorState message="Could not load employees." onRetry={() => void refetch()} />
      ) : (
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="label-field" htmlFor="assign-employee">Employee *</label>
            <select
              id="assign-employee"
              className="input-field"
              value={employeeId}
              onChange={(event) => setEmployeeId(event.target.value)}
              required
              autoFocus
            >
              <option value="">Select an employee…</option>
              {employees?.map((employee) => (
                <option key={employee.id} value={employee.id}>
                  {employee.fullName}{employee.department ? ` · ${employee.department}` : ''}
                </option>
              ))}
            </select>
            {employees?.length === 0 && (
              <p className="mt-1.5 text-xs text-amber-600">No employees found. Register an employee first.</p>
            )}
          </div>

          <div>
            <label className="label-field" htmlFor="assign-notes">Condition notes</label>
            <textarea
              id="assign-notes"
              className="input-field min-h-20 resize-y"
              value={conditionNotes}
              onChange={(event) => setConditionNotes(event.target.value)}
              placeholder="e.g. Sealed box, no visible damage"
              maxLength={1000}
            />
          </div>

          <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
            <button type="button" className="btn-secondary" onClick={onClose} disabled={loading}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={loading || !employeeId}>
              {loading ? 'Assigning…' : 'Assign asset'}
            </button>
          </div>
        </form>
      )}
    </Modal>
  )
}
