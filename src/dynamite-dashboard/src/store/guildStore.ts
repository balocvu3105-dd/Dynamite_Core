import { create } from 'zustand'
import type { GuildSummary, GuildInfo } from '@/types'

interface GuildState {
    selectedGuild: GuildSummary | null
    guildInfo: GuildInfo | null
    setSelectedGuild: (guild: GuildSummary) => void
    setGuildInfo: (info: GuildInfo) => void
    clear: () => void
}

export const useGuildStore = create<GuildState>((set) => ({
    selectedGuild: null,
    guildInfo: null,
    setSelectedGuild: (guild) => set({ selectedGuild: guild }),
    setGuildInfo: (info) => set({ guildInfo: info }),
    clear: () => set({ selectedGuild: null, guildInfo: null }),
}))