import api from './client'

export interface LeaderboardUser {
  rank: number
  userId: string
  coins: number
}

export interface UserWalletInfo {
  userId: string
  coins: number
  dailyStreak: number
  lastDaily?: string
}

export const economyApi = {
  getLeaderboard: (guildId: string) =>
    api.get<LeaderboardUser[]>(`/api/guilds/${guildId}/economy/leaderboard`).then((r) => r.data),

  getUserWallet: (guildId: string, userId: string) =>
    api.get<UserWalletInfo>(`/api/guilds/${guildId}/economy/users/${userId}`).then((r) => r.data),

  updateBalance: (guildId: string, userId: string, coins: number) =>
    api.put(`/api/guilds/${guildId}/economy/users/${userId}/balance`, { coins }).then((r) => r.data),
}
