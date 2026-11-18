import { useState } from 'react'
import type { FormEvent } from 'react'
import { UserPlus, Users } from 'lucide-react'
import { Modal } from '../components/common/Modal'
import { ErrorState } from '../components/common/ErrorState'
import { LoadingSpinner } from '../components/common/LoadingSpinner'
import { ROLE_LABELS, USER_ROLES } from '../constants'
import { useUsers } from '../hooks/useUsers'
import { useInviteUser } from '../hooks/useTeam'
import type { UserRole } from '../types'

export function TeamPage() {
  const { data: users, isLoading, isError, refetch } = useUsers()
  const inviteMutation = useInviteUser()

  const [open, setOpen] = useState(false)
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<UserRole>('Employee')
  const [department, setDepartment] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    await inviteMutation.mutateAsync({
      fullName: fullName.trim(),
      email: email.trim().toLowerCase(),
      password,
      role,
      department: department.trim() || null
    })
    setOpen(false)
    setFullName('')
    setEmail('')
    setPassword('')
    setDepartment('')
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 sm:text-3xl">Team</h1>
          <p className="mt-1 text-sm text-slate-500">People in your organisation and what they can access.</p>
        </div>
        <button type="button" className="btn-primary self-start" onClick={() => setOpen(true)}>
          <UserPlus className="h-4 w-4" /> Invite member
        </button>
      </div>

      <div className="card overflow-hidden">
        {isLoading ? (
          <div className="flex min-h-48 items-center justify-center"><LoadingSpinner label="Loading team…" /></div>
        ) : isError ? (
          <div className="p-4"><ErrorState message="Could not load your team." onRetry={() => void refetch()} /></div>
        ) : (
          <div className="divide-y divide-slate-100">
            {users?.map((user) => (
              <div key={user.id} className="flex flex-wrap items-center gap-3 px-5 py-4">
                <div className="flex h-9 w-9 items-center justify-center rounded-full bg-brand-100 text-sm font-bold text-brand-700">
                  {user.fullName.charAt(0).toUpperCase()}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold text-slate-800">{user.fullName}</p>
                  <p className="truncate text-xs text-slate-500">{user.email}</p>
                </div>
                <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600">
                  {ROLE_LABELS[user.role]}
                </span>
                {user.department && (
                  <span className="hidden rounded-full bg-slate-50 px-2.5 py-1 text-xs text-slate-500 sm:inline">
                    {user.department}
                  </span>
                )}
              </div>
            ))}
            {users?.length === 0 && (
              <p className="px-5 py-10 text-center text-sm text-slate-500">No team members yet.</p>
            )}
          </div>
        )}
      </div>

      <Modal open={open} onClose={() => setOpen(false)} title="Invite team member">
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="label-field" htmlFor="invite-name">Full name</label>
            <input id="invite-name" className="input-field" value={fullName}
              onChange={(event) => setFullName(event.target.value)} required autoFocus />
          </div>
          <div>
            <label className="label-field" htmlFor="invite-email">Email</label>
            <input id="invite-email" type="email" className="input-field" value={email}
              onChange={(event) => setEmail(event.target.value)} required />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="label-field" htmlFor="invite-role">Role</label>
              <select id="invite-role" className="input-field" value={role}
                onChange={(event) => setRole(event.target.value as UserRole)}>
                {USER_ROLES.map((item) => <option key={item} value={item}>{ROLE_LABELS[item]}</option>)}
              </select>
            </div>
            <div>
              <label className="label-field" htmlFor="invite-department">Department</label>
              <input id="invite-department" className="input-field" value={department}
                onChange={(event) => setDepartment(event.target.value)} placeholder="Optional" />
            </div>
          </div>
          <div>
            <label className="label-field" htmlFor="invite-password">Temporary password</label>
            <input id="invite-password" type="password" className="input-field" value={password}
              onChange={(event) => setPassword(event.target.value)} minLength={8} required />
            <p className="mt-1 text-xs text-slate-500">Share this with the member securely. They can change it later.</p>
          </div>
          <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
            <button type="button" className="btn-secondary" onClick={() => setOpen(false)} disabled={inviteMutation.isPending}>
              Cancel
            </button>
            <button type="submit" className="btn-primary" disabled={inviteMutation.isPending}>
              <Users className="h-4 w-4" /> {inviteMutation.isPending ? 'Inviting…' : 'Invite member'}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  )
}
