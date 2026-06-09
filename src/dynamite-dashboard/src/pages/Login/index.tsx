import { discordOAuthUrl } from '@/lib/utils'
import { Button } from '@/components/ui'

export default function LoginPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-[--color-surface]">
      {/* Background decoration */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-96 h-96 rounded-full bg-[--color-brand]/5 blur-3xl" />
        <div className="absolute -bottom-40 -left-40 w-96 h-96 rounded-full bg-[--color-brand]/5 blur-3xl" />
      </div>

      <div className="relative z-10 flex flex-col items-center gap-8 text-center px-4">
        {/* Logo */}
        <div className="flex flex-col items-center gap-3">
          <div className="w-16 h-16 rounded-2xl bg-[--color-brand] flex items-center justify-center shadow-lg shadow-[--color-brand]/30">
            <span className="text-white text-2xl font-bold">D</span>
          </div>
          <div>
            <h1 className="text-3xl font-bold text-[--color-text] tracking-tight">
              Dynamite
            </h1>
            <p className="text-[--color-text-muted] text-sm mt-1">
              Discord Bot Dashboard
            </p>
          </div>
        </div>

        {/* Card */}
        <div className="w-full max-w-sm bg-[--color-surface-alt] border border-[--color-border] rounded-xl p-8 flex flex-col gap-6 shadow-xl">
          <div>
            <h2 className="text-lg font-semibold text-[--color-text]">
              Welcome back
            </h2>
            <p className="text-sm text-[--color-text-muted] mt-1">
              Sign in with Discord to manage your servers
            </p>
          </div>

          <Button
            size="lg"
            className="w-full gap-3"
            onClick={() => window.location.href = discordOAuthUrl()}
          >
            {/* Discord logo SVG */}
            <svg width="20" height="20" viewBox="0 0 71 55" fill="currentColor">
              <path d="M60.1 4.9A58.5 58.5 0 0 0 45.6.4a.2.2 0 0 0-.2.1 40.8 40.8 0 0 0-1.8 3.7 54 54 0 0 0-16.2 0A37.8 37.8 0 0 0 25.5.5a.2.2 0 0 0-.2-.1 58.4 58.4 0 0 0-14.5 4.5.2.2 0 0 0-.1.1C1.6 18.7-.8 32.2.3 45.5a.2.2 0 0 0 .1.2 58.8 58.8 0 0 0 17.7 9 .2.2 0 0 0 .2-.1 42 42 0 0 0 3.6-5.9.2.2 0 0 0-.1-.3 38.7 38.7 0 0 1-5.5-2.6.2.2 0 0 1 0-.4l1.1-.8a.2.2 0 0 1 .2 0c11.5 5.3 24 5.3 35.4 0a.2.2 0 0 1 .2 0l1.1.8a.2.2 0 0 1 0 .4 36.1 36.1 0 0 1-5.5 2.6.2.2 0 0 0-.1.3 47.1 47.1 0 0 0 3.6 5.9.2.2 0 0 0 .2.1 58.6 58.6 0 0 0 17.8-9 .2.2 0 0 0 .1-.2c1.3-15.4-2.2-28.8-9.3-40.6a.2.2 0 0 0-.1-.1ZM23.7 37.8c-3.5 0-6.4-3.2-6.4-7.2s2.8-7.2 6.4-7.2 6.5 3.2 6.4 7.2c0 4-2.9 7.2-6.4 7.2Zm23.6 0c-3.5 0-6.4-3.2-6.4-7.2s2.8-7.2 6.4-7.2 6.5 3.2 6.4 7.2c0 4-2.8 7.2-6.4 7.2Z" />
            </svg>
            Continue with Discord
          </Button>

          <p className="text-xs text-[--color-text-muted]">
            By signing in you agree to our terms of service.
            We only request permissions needed to manage your servers.
          </p>
        </div>
      </div>
    </div>
  )
}
