import { clsx, type ClassValue } from 'clsx'

export function cn(...inputs: ClassValue[]) {
  return clsx(inputs)
}

export function avatarUrl(user: { id: string; avatar: string | null }) {
  if (!user.avatar) return `https://cdn.discordapp.com/embed/avatars/0.png`
  if (user.avatar.startsWith('http://') || user.avatar.startsWith('https://')) {
    return user.avatar
  }
  return `https://cdn.discordapp.com/avatars/${user.id}/${user.avatar}.png`
}

export function guildIconUrl(guild: { id: string; iconUrl: string | null }) {
  if (!guild.iconUrl) return `https://cdn.discordapp.com/embed/avatars/0.png`
  if (guild.iconUrl.startsWith('http://') || guild.iconUrl.startsWith('https://')) {
    return guild.iconUrl
  }
  return `https://cdn.discordapp.com/icons/${guild.id}/${guild.iconUrl}.png`
}

export function formatDate(iso: string) {
  return new Intl.DateTimeFormat('en-US', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(iso))
}

export function discordOAuthUrl() {
  const clientId = import.meta.env.VITE_DISCORD_CLIENT_ID || "1514609829610782862"
  const redirectUri = encodeURIComponent(
    import.meta.env.VITE_DISCORD_REDIRECT_URI || "http://localhost:5173/auth/callback"
  )
  const scope = encodeURIComponent("identify email guilds")
  return `https://discord.com/oauth2/authorize?client_id=${clientId}&redirect_uri=${redirectUri}&response_type=code&scope=${scope}`
}
