import { Link } from 'react-router-dom'
import { Compass } from 'lucide-react'

export function NotFoundPage() {
  return (
    <div className="card flex min-h-96 flex-col items-center justify-center gap-4 p-6 text-center">
      <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-slate-100 text-slate-400">
        <Compass className="h-8 w-8" />
      </div>
      <div>
        <p className="text-4xl font-bold text-slate-900">404</p>
        <h1 className="mt-1 text-lg font-semibold text-slate-800">Page not found</h1>
        <p className="mt-1 text-sm text-slate-500">The page you're looking for doesn't exist.</p>
      </div>
      <Link to="/dashboard" className="btn-primary">Back to dashboard</Link>
    </div>
  )
}
