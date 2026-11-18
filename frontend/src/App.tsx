import { lazy, Suspense } from 'react'
import type { ReactNode } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './components/auth/ProtectedRoute'
import { LoadingSpinner } from './components/common/LoadingSpinner'
import { AppLayout } from './components/layout/AppLayout'
import { useAuth } from './context/AuthContext'
import { AssetDetailPage } from './pages/AssetDetailPage'
import { AssetsPage } from './pages/AssetsPage'
import { DashboardPage } from './pages/DashboardPage'
import { ForgotPasswordPage } from './pages/ForgotPasswordPage'
import { LabelsPage } from './pages/LabelsPage'
import { LoginPage } from './pages/LoginPage'
import { MyAssetsPage } from './pages/MyAssetsPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { ResetPasswordPage } from './pages/ResetPasswordPage'
import { SignupCompanyPage } from './pages/SignupCompanyPage'
import { TeamPage } from './pages/TeamPage'

// html5-qrcode is large and only needed on the scanner route.
const ScannerPage = lazy(() =>
  import('./pages/ScannerPage').then((module) => ({ default: module.ScannerPage }))
)

function SessionGate({ children }: { children: ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <LoadingSpinner size="lg" label="Restoring your session…" />
      </div>
    )
  }

  return isAuthenticated ? <Navigate to="/dashboard" replace /> : children
}

function LoginRoute() {
  return <SessionGate><LoginPage /></SessionGate>
}

function SignupRoute() {
  return <SessionGate><SignupCompanyPage /></SessionGate>
}

// Employees see their own assets instead of the organisation-wide dashboard.
function DashboardRoute() {
  const { hasRole } = useAuth()
  return hasRole('SuperAdmin', 'ITManager') ? <DashboardPage /> : <MyAssetsPage />
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginRoute />} />
      <Route path="/signup" element={<SignupRoute />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<DashboardRoute />} />
          <Route path="/assets" element={<AssetsPage />} />
          <Route path="/my-assets" element={<MyAssetsPage />} />
          <Route path="/assets/:id" element={<AssetDetailPage />} />
          <Route path="/labels" element={<LabelsPage />} />

          <Route element={<ProtectedRoute allowedRoles={['ITManager', 'SuperAdmin']} />}>
            <Route
              path="/scan"
              element={
                <Suspense fallback={<div className="card flex min-h-64 items-center justify-center"><LoadingSpinner label="Loading scanner…" /></div>}>
                  <ScannerPage />
                </Suspense>
              }
            />
          </Route>

          <Route element={<ProtectedRoute allowedRoles={['SuperAdmin']} />}>
            <Route path="/team" element={<TeamPage />} />
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>
    </Routes>
  )
}
