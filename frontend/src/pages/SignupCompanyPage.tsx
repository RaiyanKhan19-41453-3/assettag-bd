import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Building2, Lock, Mail, ScanLine, User } from 'lucide-react'
import { useAuth } from '../context/AuthContext'
import { getErrorMessage } from '../services/api'

export function SignupCompanyPage() {
  const [companyName, setCompanyName] = useState('')
  const [adminFullName, setAdminFullName] = useState('')
  const [adminEmail, setAdminEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const { signupCompany } = useAuth()
  const navigate = useNavigate()

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError('')
    setSubmitting(true)

    try {
      await signupCompany({
        companyName: companyName.trim(),
        adminFullName: adminFullName.trim(),
        adminEmail: adminEmail.trim().toLowerCase(),
        password
      })
      navigate('/dashboard', { replace: true })
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-10">
      <div className="w-full max-w-lg">
        <div className="mb-6 flex items-center justify-center gap-2.5">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-brand-600 text-white">
            <ScanLine className="h-5 w-5" />
          </div>
          <span className="text-xl font-bold text-slate-900">AssetTag BD</span>
        </div>

        <div className="card p-6 sm:p-8">
          <h1 className="text-2xl font-bold text-slate-900">Create your workspace</h1>
          <p className="mt-1.5 text-sm text-slate-500">
            Set up your organisation in under a minute. Your data is isolated from every other company.
          </p>

          <form onSubmit={handleSubmit} className="mt-6 space-y-4">
            {error && (
              <div className="rounded-lg border border-red-200 bg-red-50 px-3.5 py-3 text-sm text-red-700" role="alert">
                {error}
              </div>
            )}

            <div>
              <label className="label-field" htmlFor="company-name">Company name</label>
              <div className="relative">
                <Building2 className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <input
                  id="company-name"
                  className="input-field pl-10"
                  placeholder="e.g. Acme Bangladesh Ltd."
                  value={companyName}
                  onChange={(event) => setCompanyName(event.target.value)}
                  required
                  maxLength={200}
                  autoFocus
                />
              </div>
            </div>

            <div>
              <label className="label-field" htmlFor="admin-name">Your full name</label>
              <div className="relative">
                <User className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <input
                  id="admin-name"
                  className="input-field pl-10"
                  placeholder="e.g. Alice Rahman"
                  value={adminFullName}
                  onChange={(event) => setAdminFullName(event.target.value)}
                  required
                  maxLength={150}
                />
              </div>
            </div>

            <div>
              <label className="label-field" htmlFor="admin-email">Work email</label>
              <div className="relative">
                <Mail className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <input
                  id="admin-email"
                  type="email"
                  className="input-field pl-10"
                  placeholder="you@company.com"
                  value={adminEmail}
                  onChange={(event) => setAdminEmail(event.target.value)}
                  required
                />
              </div>
            </div>

            <div>
              <label className="label-field" htmlFor="signup-password">Password</label>
              <div className="relative">
                <Lock className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <input
                  id="signup-password"
                  type="password"
                  className="input-field pl-10"
                  placeholder="At least 8 characters"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  minLength={8}
                  required
                />
              </div>
            </div>

            <button type="submit" className="btn-primary w-full py-3" disabled={submitting}>
              {submitting ? 'Creating workspace…' : 'Create workspace'}
            </button>
          </form>

          <p className="mt-5 text-center text-sm text-slate-500">
            Already have an account?{' '}
            <Link to="/login" className="font-semibold text-brand-600 hover:underline">Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  )
}
