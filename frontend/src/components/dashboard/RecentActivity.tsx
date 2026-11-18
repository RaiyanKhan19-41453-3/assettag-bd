import { ClipboardCheck, PackagePlus, Undo2 } from 'lucide-react'
import type { Activity } from '../../types'

const iconForType = {
  Assignment: PackagePlus,
  Return: Undo2,
  Maintenance: ClipboardCheck
} as const

const iconTone = {
  Assignment: 'bg-blue-50 text-blue-600',
  Return: 'bg-emerald-50 text-emerald-600',
  Maintenance: 'bg-amber-50 text-amber-600'
} as const

function relativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime()
  const minutes = Math.floor(diff / 60_000)
  if (minutes < 1) return 'Just now'
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  if (days < 30) return `${days}d ago`
  return new Date(iso).toLocaleDateString()
}

export function RecentActivity({ activities }: { activities: Activity[] }) {
  if (activities.length === 0) {
    return <p className="px-5 py-8 text-center text-sm text-slate-500">No recent activity yet.</p>
  }

  return (
    <ul className="divide-y divide-slate-100">
      {activities.map((activity) => {
        const Icon = iconForType[activity.type]
        return (
          <li key={`${activity.type}-${activity.id}`} className="flex gap-3 px-5 py-3.5">
            <div className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-lg ${iconTone[activity.type]}`}>
              <Icon className="h-4 w-4" />
            </div>
            <div className="min-w-0 flex-1">
              <p className="text-sm text-slate-700">{activity.description}</p>
              <p className="mt-0.5 text-xs text-slate-400">{relativeTime(activity.occurredAt)}</p>
            </div>
          </li>
        )
      })}
    </ul>
  )
}
