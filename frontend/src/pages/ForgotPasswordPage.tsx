import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, Mail } from 'lucide-react'
import { getErrorMessage } from '../services/api'
import { authService } from '../services/auth.service'

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [devToken, setDevToken] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      const result = await authService.forgotPassword(email.trim().toLowerCase())
      setMessage(result.message)
      setDevToken(result.devToken ?? null)
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

        <h1 className="mt-5 text-2xl font-bold text-slate-900">Reset your password</h1>
        <p className="mt-1.5 text-sm text-slate-500">
          Enter your work email and we'll send you a reset link.
        </p>

        {message && (
          <div className="mt-5 rounded-lg border border-emerald-200 bg-emerald-50 px-3.5 py-3 text-sm text-emerald-700">
            {message}
            {devToken && (
              <p className="mt-2 text-xs">
                Development mode:{' '}
                <Link
                  className="font-semibold underline"
                  to={`/reset-password?email=${encodeURIComponent(email)}&token=${encodeURIComponent(devToken)}`}
                >
                  open the reset link
                </Link>
              </p>
            )}
          </div>
        )}

        {error && (
          <div className="mt-5 rounded-lg border border-red-200 bg-red-50 px-3.5 py-3 text-sm text-red-700" role="alert">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-6 space-y-4">
          <div>
            <label className="label-field" htmlFor="forgot-email">Email address</label>
            <div className="relative">
              <Mail className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                id="forgot-email"
                type="email"
                className="input-field pl-10"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="you@company.com"
                required
                autoFocus
              />
            </div>
          </div>
          <button type="submit" className="btn-primary w-full py-3" disabled={submitting}>
            {submitting ? 'Sending…' : 'Send reset link'}
          </button>
        </form>
      </div>
    </div>
  )
}
