import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { authApi } from '@/api'
import { useAuthStore } from '@/store/authStore'
import { Button, Spinner } from '@/components/ui'

export default function AuthCallbackPage() {
  const navigate = useNavigate()
  const login = useAuthStore((s) => s.login)
  const called = useRef(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (called.current) return
    called.current = true

    const params = new URLSearchParams(window.location.search)
    const code = params.get('code')
    const state = params.get('state')
    const savedState = sessionStorage.getItem('oauth_state')

    if (savedState && state !== savedState) {
      setError('Cảnh báo bảo mật: OAuth State không khớp (Có thể do tấn công CSRF). Vui lòng thử đăng nhập lại.')
      return
    }
    if (savedState) {
      sessionStorage.removeItem('oauth_state')
    }

    if (!code) {
      navigate('/login', { replace: true })
      return
    }

    const redirectUri = import.meta.env.VITE_DISCORD_REDIRECT_URI || `${window.location.origin}/auth/callback`

    authApi
      .login(code, redirectUri)
      .then((res) => {
        login(res.user, res.accessToken, res.discordToken ?? res.accessToken)
        navigate('/servers', { replace: true })
      })
      .catch((err: any) => {
        console.error('Auth error:', err)
        const errorMessage = err.response?.data?.error || err.response?.data?.message || err.message || 'Đăng nhập thất bại. Vui lòng thử lại.'
        setError(errorMessage)
      })
  }, [login, navigate])

  if (error) {
    return (
      <div className="min-h-screen flex flex-col items-center justify-center gap-4 bg-[--color-surface] p-4 text-center">
        <div className="max-w-md w-full bg-[--color-surface-alt] border border-red-500/30 rounded-xl p-6 flex flex-col gap-4 shadow-xl animate-fadeIn">
          <h1 className="text-xl font-bold text-red-400">Đăng Nhập Thất Bại</h1>
          <p className="text-sm text-[--color-text] bg-[--color-surface] p-3 rounded border border-[--color-border] break-words">
            {error}
          </p>
          <Button onClick={() => navigate('/login', { replace: true })} className="w-full mt-2">
            Quay lại trang Đăng Nhập
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-4 bg-[--color-surface]">
      <Spinner size="lg" />
      <p className="text-[--color-text-muted] text-sm">Signing you in...</p>
    </div>
  )
}
