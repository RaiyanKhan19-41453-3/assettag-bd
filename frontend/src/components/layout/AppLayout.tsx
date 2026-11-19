import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import {
  Boxes,
  Laptop,
  LayoutDashboard,
  LogOut,
  Menu,
  QrCode,
  ScanLine,
  Users,
  X
} from 'lucide-react'
import { useAuth } from '../../context/AuthContext'
import type { UserRole } from '../../types'

interface NavItem {
  to: string
  label: string
  icon: typeof LayoutDashboard
  roles?: UserRole[]
}

const navItems: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, roles: ['SuperAdmin', 'ITManager'] },
  { to: '/assets', label: 'Assets', icon: Boxes },
  { to: '/my-assets', label: 'My assets', icon: Laptop },
  { to: '/scan', label: 'Scan QR', icon: QrCode, roles: ['ITManager', 'SuperAdmin'] },
  { to: '/team', label: 'Team', icon: Users, roles: ['SuperAdmin'] }
]

const roleLabels: Record<UserRole, string> = {
  SuperAdmin: 'Super Admin',
  ITManager: 'IT Manager',
  Employee: 'Employee'
}

export function AppLayout() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const { user, logout, hasRole } = useAuth()
  const location = useLocation()

  useEffect(() => setMobileOpen(false), [location.pathname])

  const visibleItems = navItems.filter((item) => !item.roles || hasRole(...item.roles))

  const navList = (
    <nav className="flex flex-col gap-1 px-3 py-4">
      {visibleItems.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          className={({ isActive }) =>
            `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition ${
              isActive
                ? 'bg-brand-50 text-brand-700'
                : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
            }`
          }
        >
          <item.icon className="h-5 w-5 shrink-0" />
          {item.label}
        </NavLink>
      ))}
    </nav>
  )

  const brand = (
    <div className="flex h-16 items-center gap-2.5 border-b border-slate-200 px-5">
      <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-brand-600 text-white">
        <ScanLine className="h-5 w-5" />
      </div>
      <div className="leading-tight">
        <p className="text-sm font-bold text-slate-900">AssetTag BD</p>
        <p className="text-[11px] text-slate-500">Asset tracking</p>
      </div>
    </div>
  )

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Desktop sidebar */}
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-60 flex-col border-r border-slate-200 bg-white lg:flex">
        {brand}
        <div className="flex-1 overflow-y-auto">{navList}</div>
        <div className="border-t border-slate-200 p-3">
          <button
            type="button"
            onClick={logout}
            className="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-slate-600 transition hover:bg-red-50 hover:text-red-600"
          >
            <LogOut className="h-5 w-5" />
            Sign out
          </button>
        </div>
      </aside>

      {/* Mobile drawer */}
      {mobileOpen && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <button
            type="button"
            aria-label="Close navigation"
            className="absolute inset-0 bg-slate-900/50"
            onClick={() => setMobileOpen(false)}
          />
          <aside className="absolute inset-y-0 left-0 flex w-72 flex-col bg-white shadow-xl">
            <div className="flex items-center justify-between pr-3">{brand}</div>
            <div className="flex-1 overflow-y-auto">{navList}</div>
            <div className="border-t border-slate-200 p-3">
              <button
                type="button"
                onClick={logout}
                className="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-slate-600 hover:bg-red-50 hover:text-red-600"
              >
                <LogOut className="h-5 w-5" />
                Sign out
              </button>
            </div>
          </aside>
        </div>
      )}

      {/* Content */}
      <div className="lg:pl-60">
        <header className="sticky top-0 z-20 flex h-16 items-center justify-between border-b border-slate-200 bg-white/90 px-4 backdrop-blur sm:px-6">
          <button
            type="button"
            className="rounded-lg p-2 text-slate-600 hover:bg-slate-100 lg:hidden"
            aria-label={mobileOpen ? 'Close navigation' : 'Open navigation'}
            onClick={() => setMobileOpen((open) => !open)}
          >
            {mobileOpen ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}
          </button>

          <div className="hidden sm:block">
            <p className="text-sm font-semibold text-slate-800">{user?.fullName}</p>
            <p className="text-xs text-slate-500">{user ? roleLabels[user.role] : ''}</p>
          </div>

          <div className="ml-auto flex items-center gap-3">
            <div className="hidden h-9 w-9 items-center justify-center rounded-full bg-brand-100 text-sm font-bold text-brand-700 sm:flex">
              {user?.fullName.charAt(0).toUpperCase()}
            </div>
            <button
              type="button"
              onClick={logout}
              className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-red-600 lg:hidden"
              aria-label="Sign out"
            >
              <LogOut className="h-5 w-5" />
            </button>
          </div>
        </header>

        <main className="mx-auto w-full max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
