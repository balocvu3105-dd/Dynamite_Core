import api from './client'

export interface BlacklistEntry {
  userId: string
  username: string
  avatarUrl: string | null
  reason: string
  notes: string | null
  isActive: boolean
  moderatorId: string
  createdAt: string
}

export interface AddBlacklistRequest {
  targetUserId: string
  targetUsername: string
  targetAvatarUrl?: string | null
  reason: string
  notes?: string | null
}

export interface DiscordUserLookup {
  id: string
  username: string
  avatar: string | null
}

export const blacklistApi = {
  getBlacklist: (guildId: string, count = 50) =>
    api
      .get<BlacklistEntry[]>(`/api/guilds/${guildId}/blacklist`, { params: { count } })
      .then((r) => r.data),

  addToBlacklist: (guildId: string, data: AddBlacklistRequest) =>
    api
      .post<BlacklistEntry>(`/api/guilds/${guildId}/blacklist`, data)
      .then((r) => r.data),

  removeFromBlacklist: (guildId: string, userId: string, reason: string) =>
    api
      .delete(`/api/guilds/${guildId}/blacklist/${userId}`, { data: { reason } })
      .then((r) => r.data),

  lookupUser: (guildId: string, userId: string) =>
    api
      .get<DiscordUserLookup>(`/api/guilds/${guildId}/blacklist/lookup/${userId}`)
      .then((r) => r.data),
}

