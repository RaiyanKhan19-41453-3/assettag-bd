import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ArrowLeft, Lock } from 'lucide-react'
import { getErrorMessage } from '../services/api'
import { authService } from '../services/auth.service'

export function ResetPasswordPage() {
  const [params] = useSearchParams()
  const email = params.get('email') ?? ''
  const token = params.get('token') ?? ''

  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState('')
  const [done, setDone] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError('')

    if (password !== confirm) {
      setError('Passwords do not match.')
      return
    }

    setSubmitting(true)
    try {
      await authService.resetPassword({
        email,
        token,
        newPassword: password
      })
      setDone(true)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-10">
      <div className="w-full max-w-md card p-6 sm:p-8">
        <Link to="/login" className="inline-flex items-center gap-1.5 text-sm text-brand-600 hover:underline">
          <ArrowLeft className="h-4 w-4" /> Back to sign in
        </Link>

        <h1 className="mt-5 text-2xl font-bold text-slate-900">Choose a new password</h1>
        <p className="mt-1.5 text-sm text-slate-500">Use at least 8 characters.</p>

        {done ? (
          <div className="mt-6 rounded-lg border border-emerald-200 bg-emerald-50 px-3.5 py-4 text-sm text-emerald-700">
            <p>Your password has been reset. All existing sessions were signed out.</p>
            <Link to="/login" className="mt-2 inline-block font-semibold underline">Sign in with your new password</Link>
          </div>
        ) : !email || !token ? (
          <div className="mt-6 rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-4 text-sm text-amber-700">
            This reset link is incomplete. Request a new one from the sign-in page.
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="mt-6 space-y-4">
            {error && (
              <div className="rounded-lg border border-red-200 bg-red-50 px-3.5 py-3 text-sm text-red-700" role="alert">
                {error}
              </div>
            )}
            <div>
              <label className="label-field" htmlFor="new-password">New password</label>
              <div className="relative">
                <Lock className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <input
                  id="new-password"
                  type="password"
                  className="input-field pl-10"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  minLength={8}
                  required
                  autoFocus
                />
              </div>
            </div>
            <div>
              <label className="label-field" htmlFor="confirm-password">Confirm password</label>
              <div className="relative">
                <Lock className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <input
                  id="confirm-password"
                  type="password"
                  className="input-field pl-10"
                  value={confirm}
                  onChange={(event) => setConfirm(event.target.value)}
                  minLength={8}
                  required
                />
              </div>
            </div>
            <button type="submit" className="btn-primary w-full py-3" disabled={submitting}>
              {submitting ? 'Resetting…' : 'Reset password'}
            </button>
          </form>
        )}
      </div>
    </div>
  )
}
