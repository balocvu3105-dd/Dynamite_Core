import { clsx, type ClassValue } from 'clsx'

export function cn(...inputs: ClassValue[]) {
  return clsx(inputs)
}

export function avatarUrl(user: { id: string; avatar: string | null }) {
  if (!user.avatar) return `https://cdn.discordapp.com/embed/avatars/0.png`
  return `https://cdn.discordapp.com/avatars/${user.id}/${user.avatar}.png`
}

export function guildIconUrl(guild: { id: string; iconUrl: string | null }) {
  return guild.iconUrl ?? `https://cdn.discordapp.com/embed/avatars/0.png`
}

export function formatDate(iso: string) {
  return new Intl.DateTimeFormat('en-US', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(iso))
}

export function discordOAuthUrl() {
  const clientId = import.meta.env.VITE_DISCORD_CLIENT_ID as string
  const redirectUri = encodeURIComponent(import.meta.env.VITE_DISCORD_REDIRECT_URI as string)
  const scope = encodeURIComponent('identify email guilds')
  return `https://discord.com/api/oauth2/authorize?client_id=${clientId}&redirect_uri=${redirectUri}&response_type=code&scope=${scope}`
}
