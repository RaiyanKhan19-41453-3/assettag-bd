import { AlertCircle, RotateCw } from 'lucide-react'

interface ErrorStateProps {
  message?: string
  onRetry?: () => void
}

export function ErrorState({
  message = 'Something went wrong while loading this data.',
  onRetry
}: ErrorStateProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 rounded-xl border border-red-100 bg-red-50 px-6 py-12 text-center">
      <AlertCircle className="h-8 w-8 text-red-500" />
      <p className="max-w-md text-sm text-red-700">{message}</p>
      {onRetry && (
        <button type="button" onClick={onRetry} className="btn-secondary mt-1">
          <RotateCw className="h-4 w-4" />
          Try again
        </button>
      )}
    </div>
  )
}
