import { History } from 'lucide-react'
import { EmptyState } from '../components/common/EmptyState'
import { ErrorState } from '../components/common/ErrorState'
import { LoadingSpinner } from '../components/common/LoadingSpinner'
import { formatAction, useAudit } from '../hooks/useAudit'
import { formatDateTime } from '../utils/format'

export function AuditPage() {
  const { data, isLoading, isError, refetch, pageNumber, setPageNumber } = useAudit()

  if (isLoading) {
    return <div className="card flex min-h-64 items-center justify-center"><LoadingSpinner label="Loading activity…" /></div>
  }

  if (isError) {
    return <ErrorState message="Could not load the activity log." onRetry={() => void refetch()} />
  }

  const entries = data?.items ?? []

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 sm:text-3xl">Activity log</h1>
        <p className="mt-1 text-sm text-slate-500">A record of changes made in your workspace.</p>
      </div>

      <div className="card overflow-hidden">
        {entries.length === 0 ? (
          <EmptyState title="No activity yet" description="Changes will be recorded here as your team works." />
        ) : (
          <ul className="divide-y divide-slate-100">
            {entries.map((entry) => (
              <li key={entry.id} className="flex flex-col gap-2 px-5 py-4 sm:flex-row sm:items-start sm:gap-4">
                <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
                  <History className="h-4 w-4" />
                </div>
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-semibold text-slate-800">
                    {formatAction(entry.action)}
                    <span className="ml-2 font-normal text-slate-400">· {entry.entityType}</span>
                  </p>
                  <p className="mt-0.5 text-xs text-slate-500">
                    {entry.actor} · {formatDateTime(entry.occurredAt)}
                  </p>
                  {entry.details && (
                    <details className="mt-2 text-xs text-slate-500">
                      <summary className="cursor-pointer hover:text-slate-700">Details</summary>
                      <pre className="mt-1 overflow-x-auto rounded bg-slate-50 p-2 text-[11px]">{entry.details}</pre>
                    </details>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}

        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-between border-t border-slate-200 px-5 py-3 text-xs text-slate-500">
            <span>Page {pageNumber} of {data.totalPages}</span>
            <div className="flex gap-2">
              <button type="button" className="btn-secondary px-3 py-1.5 text-xs"
                disabled={pageNumber <= 1} onClick={() => setPageNumber(pageNumber - 1)}>Previous</button>
              <button type="button" className="btn-secondary px-3 py-1.5 text-xs"
                disabled={pageNumber >= data.totalPages} onClick={() => setPageNumber(pageNumber + 1)}>Next</button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
