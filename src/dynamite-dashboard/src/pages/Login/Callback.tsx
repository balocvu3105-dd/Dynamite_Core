import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { authApi } from '@/api'
import { useAuthStore } from '@/store/authStore'
import { Spinner } from '@/components/ui'

export default function AuthCallbackPage() {
  const navigate = useNavigate()
  const login = useAuthStore((s) => s.login)
  const called = useRef(false)

  useEffect(() => {
    // Strict mode runs effects twice — guard with ref
    if (called.current) return
    called.current = true

    const params = new URLSearchParams(window.location.search)
    const code = params.get('code')

    if (!code) {
      navigate('/login', { replace: true })
      return
    }

    authApi
      .login(code)
      .then((res) => {
        // Store discord token separately — sent as header on every API request
        login(res.user, res.accessToken, res.discordToken ?? res.accessToken)
        navigate('/servers', { replace: true })
      })
      .catch(() => navigate('/login', { replace: true }))
  }, [login, navigate])

  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-4 bg-[--color-surface]">
      <Spinner size="lg" />
      <p className="text-[--color-text-muted] text-sm">Signing you in...</p>
    </div>
  )
}
