import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Eye, EyeOff, Lock, Mail, ScanLine } from 'lucide-react'
import { useAuth } from '../context/AuthContext'
import { getErrorMessage } from '../services/api'

export function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const { login } = useAuth()
  const navigate = useNavigate()

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError('')
    setSubmitting(true)

    try {
      const user = await login({ email: email.trim().toLowerCase(), password })
      navigate(user.role === 'Employee' ? '/my-assets' : '/dashboard', { replace: true })
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen bg-white">
      {/* Branding panel */}
      <div className="relative hidden w-1/2 flex-col justify-between overflow-hidden bg-brand-700 px-12 py-12 text-white lg:flex">
        <div className="absolute -right-24 -top-24 h-96 w-96 rounded-full bg-brand-500/40 blur-3xl" />
        <div className="absolute -bottom-32 -left-20 h-96 w-96 rounded-full bg-brand-900/50 blur-3xl" />

        <div className="relative flex items-center gap-3">
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-white/15 backdrop-blur">
            <ScanLine className="h-6 w-6" />
          </div>
          <div>
            <p className="text-lg font-bold">AssetTag BD</p>
            <p className="text-xs text-brand-200">Corporate asset tracking</p>
          </div>
        </div>

        <div className="relative max-w-md">
          <h1 className="text-4xl font-bold leading-tight">
            Know what you own.<br />Know who has it.
          </h1>
          <p className="mt-5 text-base leading-relaxed text-brand-100">
            Replace messy Excel sheets with QR-tagged assets, instant checkouts and a clear audit trail — built for
            offices across Bangladesh.
          </p>
          <div className="mt-8 flex flex-wrap gap-2 text-xs font-medium text-brand-100">
            {['QR scanning', 'Assignment workflow', 'Maintenance logs', 'Live dashboard'].map((item) => (
              <span key={item} className="rounded-full border border-white/20 bg-white/10 px-3 py-1.5">
                {item}
              </span>
            ))}
          </div>
        </div>

        <p className="relative text-xs text-brand-200">© {new Date().getFullYear()} AssetTag BD. All rights reserved.</p>
      </div>

      {/* Form panel */}
      <div className="flex w-full flex-col justify-center px-6 py-12 sm:px-12 lg:w-1/2 xl:px-24">
        <div className="mx-auto w-full max-w-sm">
          <div className="mb-8 flex items-center gap-2.5 lg:hidden">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-brand-600 text-white">
              <ScanLine className="h-5 w-5" />
            </div>
            <span className="text-lg font-bold text-slate-900">AssetTag BD</span>
          </div>

          <h2 className="text-2xl font-bold text-slate-900">Welcome back</h2>
          <p className="mt-1.5 text-sm text-slate-500">Sign in to manage your organisation's assets.</p>

          <form onSubmit={handleSubmit} className="mt-8 space-y-5">
            {error && (
              <div className="rounded-lg border border-red-200 bg-red-50 px-3.5 py-3 text-sm text-red-700" role="alert">
                {error}
              </div>
            )}

            <div>
              <label className="label-field" htmlFor="email">Email address</label>
              <div className="relative">
                <Mail className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <input
                  id="email"
                  type="email"
                  autoComplete="email"
                  className="input-field pl-10"
                  placeholder="you@company.com"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  required
                  autoFocus
                />
              </div>
            </div>

            <div>
              <label className="label-field" htmlFor="password">Password</label>
              <div className="relative">
                <Lock className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  className="input-field pl-10 pr-11"
                  placeholder="Enter your password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  required
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((visible) => !visible)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 rounded p-1 text-slate-400 hover:text-slate-600"
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              <div className="mt-1.5 text-right">
                <Link to="/forgot-password" className="text-xs font-medium text-brand-600 hover:underline">
                  Forgot password?
                </Link>
              </div>
            </div>

            <button type="submit" className="btn-primary w-full py-3" disabled={submitting}>
              {submitting ? 'Signing in…' : 'Sign in'}
            </button>
          </form>

          {(import.meta.env.DEV || import.meta.env.VITE_SHOW_DEMO_CREDENTIALS === 'true') && (
            <div className="mt-8 rounded-xl border border-slate-200 bg-slate-50 p-4">
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Demo accounts</p>
              <dl className="mt-2 space-y-1 text-xs text-slate-600">
                <div className="flex justify-between gap-3">
                  <dt>Administrator</dt>
                  <dd className="font-mono">
                    {import.meta.env.VITE_DEMO_EMAIL || 'admin@assettagbd.local'} /{' '}
                    {import.meta.env.VITE_DEMO_PASSWORD || 'Admin@123'}
                  </dd>
                </div>
                {import.meta.env.VITE_DEMO_SAMPLE === 'true' && (
                  <>
                    <div className="flex justify-between gap-3">
                      <dt>IT Manager</dt><dd className="font-mono">rahim@assettagbd.local / Manager@123</dd>
                    </div>
                    <div className="flex justify-between gap-3">
                      <dt>Employee</dt><dd className="font-mono">sadia@assettagbd.local / Employee@123</dd>
                    </div>
                  </>
                )}
              </dl>
              <p className="mt-2 text-[11px] text-slate-400">
                Values come from the deployment environment. Enable SEED_SAMPLE_DATA for the extra accounts.
              </p>
            </div>
          )}

          <p className="mt-6 text-center text-sm text-slate-500">
            New here?{' '}
            <Link to="/signup" className="font-semibold text-brand-600 hover:underline">
              Create a company workspace
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
