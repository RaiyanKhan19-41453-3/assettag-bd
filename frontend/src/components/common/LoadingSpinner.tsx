interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg'
  label?: string
}

const sizeClasses = {
  sm: 'h-4 w-4 border-2',
  md: 'h-7 w-7 border-[3px]',
  lg: 'h-10 w-10 border-4'
}

export function LoadingSpinner({ size = 'md', label }: LoadingSpinnerProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-3" role="status" aria-live="polite">
      <div className={`animate-spin rounded-full border-brand-200 border-t-brand-600 ${sizeClasses[size]}`} />
      {label && <p className="text-sm text-slate-500">{label}</p>}
      <span className="sr-only">Loading</span>
    </div>
  )
}
