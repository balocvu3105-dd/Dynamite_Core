import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { Bot, ExternalLink, LogOut } from 'lucide-react'
import { guildsApi } from '@/api'
import { useAuthStore } from '@/store/authStore'
import { useGuildStore } from '@/store/guildStore'
import { avatarUrl, guildIconUrl } from '@/lib/utils'
import { Badge, Button, Spinner, Card } from '@/components/ui'
import type { GuildSummary } from '@/types'

export default function ServersPage() {
  const navigate = useNavigate()
  const { user, logout } = useAuthStore()
  const setSelectedGuild = useGuildStore((s) => s.setSelectedGuild)

  const { data: guilds, isLoading, isError } = useQuery({
    queryKey: ['guilds'],
    queryFn: guildsApi.list,
  })

  const handleSelectGuild = (guild: GuildSummary) => {
    if (!guild.botPresent) return
    setSelectedGuild(guild)
    navigate(`/dashboard/${guild.id}/overview`)
  }

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="min-h-screen bg-[--color-surface]">
      <header className="border-b border-[--color-border] bg-[--color-surface-alt]">
        <div className="max-w-5xl mx-auto px-6 h-14 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-7 h-7 rounded-md bg-[--color-brand] flex items-center justify-center">
              <span className="text-white text-xs font-bold">D</span>
            </div>
            <span className="font-semibold text-[--color-text]">Dynamite</span>
          </div>
          {user && (
            <div className="flex items-center gap-3">
              <img src={avatarUrl(user)} alt={user.username} className="w-7 h-7 rounded-full" />
              <span className="text-sm text-[--color-text-muted]">{user.username}</span>
              <Button variant="ghost" size="sm" onClick={handleLogout}>
                <LogOut size={14} />
              </Button>
            </div>
          )}
        </div>
      </header>

      <div className="max-w-5xl mx-auto px-6 py-10">
        <div className="mb-8">
          <h1 className="text-2xl font-bold text-[--color-text]">Your Servers</h1>
          <p className="text-[--color-text-muted] mt-1 text-sm">
            Select a server to manage. Servers without the bot cannot be configured.
          </p>
        </div>

        {isLoading && (
          <div className="flex items-center justify-center py-20">
            <Spinner size="lg" />
          </div>
        )}

        {isError && (
          <Card className="text-center py-10">
            <p className="text-[--color-danger] font-medium">Failed to load servers</p>
            <p className="text-[--color-text-muted] text-sm mt-1">Make sure the API is running</p>
          </Card>
        )}

        {guilds && (
          <>
            {guilds.filter((g) => g.botPresent).length > 0 && (
              <section className="mb-8">
                <h2 className="text-xs font-semibold text-[--color-text-muted] uppercase tracking-wider mb-3">
                  Bot installed
                </h2>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                  {guilds.filter((g) => g.botPresent).map((guild) => (
                    <GuildCard key={guild.id} guild={guild} onClick={() => handleSelectGuild(guild)} />
                  ))}
                </div>
              </section>
            )}
            {guilds.filter((g) => !g.botPresent).length > 0 && (
              <section>
                <h2 className="text-xs font-semibold text-[--color-text-muted] uppercase tracking-wider mb-3">
                  Bot not installed
                </h2>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                  {guilds.filter((g) => !g.botPresent).map((guild) => (
                    <GuildCard key={guild.id} guild={guild} onClick={() => handleSelectGuild(guild)} />
                  ))}
                </div>
              </section>
            )}
          </>
        )}
      </div>
    </div>
  )
}

function GuildCard({ guild, onClick }: { guild: GuildSummary; onClick: () => void }) {
  const botInviteUrl = `https://discord.com/api/oauth2/authorize?client_id=${import.meta.env.VITE_DISCORD_CLIENT_ID}&permissions=8&scope=bot%20applications.commands&guild_id=${guild.id}`

  return (
    <Card
      className={`flex items-center gap-3 transition-all ${guild.botPresent
          ? 'hover:border-[--color-brand]/50 hover:bg-[--color-surface-raised] cursor-pointer'
          : 'opacity-60'
        }`}
      onClick={guild.botPresent ? onClick : undefined}
    >
      <img
        src={guildIconUrl(guild)}
        alt={guild.name}
        className="w-10 h-10 rounded-full flex-shrink-0"
      />
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium text-[--color-text] truncate">{guild.name}</p>
        <div className="mt-0.5">
          {guild.botPresent ? (
            <Badge variant="success"><Bot size={10} />Active</Badge>
          ) : (
            <a
              href={botInviteUrl}
              target="_blank"
              rel="noopener noreferrer"
              onClick={(e) => e.stopPropagation()}
              className="inline-flex items-center gap-1 text-xs text-[--color-brand] hover:underline"
            >
              Add bot <ExternalLink size={10} />
            </a>
          )}
        </div>
      </div>
    </Card>
  )
}