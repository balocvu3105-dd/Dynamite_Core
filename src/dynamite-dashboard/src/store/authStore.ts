import { create } from 'zustand'
import type { DiscordUser } from '@/types'

interface AuthState {
  user: DiscordUser | null
  accessToken: string | null
  discordToken: string | null
  isAuthenticated: boolean
  login: (user: DiscordUser, accessToken: string, discordToken: string) => void
  logout: () => void
  hydrate: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  discordToken: null,
  isAuthenticated: false,

  login: (user, accessToken, discordToken) => {
    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem('discordToken', discordToken)
    localStorage.setItem('user', JSON.stringify(user))
    set({ user, accessToken, discordToken, isAuthenticated: true })
  },

  logout: () => {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('discordToken')
    localStorage.removeItem('user')
    set({ user: null, accessToken: null, discordToken: null, isAuthenticated: false })
  },

  // Re-hydrate from localStorage on app load
  hydrate: () => {
    const token = localStorage.getItem('accessToken')
    const discordToken = localStorage.getItem('discordToken')
    const raw = localStorage.getItem('user')
    if (token && discordToken && raw) {
      try {
        const user = JSON.parse(raw) as DiscordUser
        set({ user, accessToken: token, discordToken, isAuthenticated: true })
      } catch {
        localStorage.removeItem('accessToken')
        localStorage.removeItem('discordToken')
        localStorage.removeItem('user')
      }
    }
  },
}))
