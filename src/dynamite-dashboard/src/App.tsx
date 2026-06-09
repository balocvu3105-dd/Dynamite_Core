import { useEffect, lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useAuthStore } from '@/store/authStore'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { DashboardLayout } from '@/components/layout/DashboardLayout'
import { Spinner } from '@/components/ui'

const LoginPage = lazy(() => import('@/pages/Login'))
const CallbackPage = lazy(() => import('@/pages/Login/Callback'))
const ServersPage = lazy(() => import('@/pages/Servers'))
const OverviewPage = lazy(() => import('@/pages/Dashboard/Overview'))
const ModerationPage = lazy(() => import('@/pages/Dashboard/Moderation'))
const LoggingPage = lazy(() => import('@/pages/Dashboard/Logging'))
const WelcomePage = lazy(() => import('@/pages/Dashboard/Welcome'))
const SecurityPage = lazy(() => import('@/pages/Dashboard/Security'))

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } },
})

function PageLoader() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-[--color-surface]">
      <Spinner size="lg" />
    </div>
  )
}

export default function App() {
  const hydrate = useAuthStore((s) => s.hydrate)
  useEffect(() => { hydrate() }, [hydrate])

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Suspense fallback={<PageLoader />}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/auth/callback" element={<CallbackPage />} />

            <Route element={<ProtectedRoute />}>
              <Route path="/servers" element={<ServersPage />} />

              <Route path="/dashboard/:guildId" element={<DashboardLayout />}>
                <Route index element={<Navigate to="overview" replace />} />
                <Route path="overview" element={<OverviewPage />} />
                <Route path="moderation" element={<ModerationPage />} />
                <Route path="logging" element={<LoggingPage />} />
                <Route path="welcome" element={<WelcomePage />} />
                <Route path="security" element={<SecurityPage />} />
              </Route>
            </Route>

            <Route path="*" element={<Navigate to="/login" replace />} />
          </Routes>
        </Suspense>
      </BrowserRouter>
    </QueryClientProvider>
  )
}