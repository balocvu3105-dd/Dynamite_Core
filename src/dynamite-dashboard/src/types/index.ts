// ── Auth ─────────────────────────────────────────────────────────────────────
export interface AuthResponse {
  accessToken: string
  discordToken: string
  refreshToken: string
  expiresIn: number
  user: DiscordUser
}

export interface DiscordUser {
  id: string
  username: string
  discriminator: string
  avatar: string | null
  email: string
}

// Alias — UserProfile is the same shape as DiscordUser but semantically
// represents the currently-authenticated user's profile across the dashboard.
export type UserProfile = DiscordUser

// ── Guild ─────────────────────────────────────────────────────────────────────
export interface GuildSummary {
  id: string
  name: string
  iconUrl: string | null
  botPresent: boolean
}

export interface GuildInfo {
  id: string
  name: string
  iconUrl: string | null
  botPresent: boolean
  channels: Channel[]
  roles: Role[]
}

export interface Channel {
  id: string
  name: string
  type: 'text' | 'voice' | 'category' | 'announcement'
}

export interface Role {
  id: string
  name: string
  color: string
  isManaged: boolean
}

export interface ModuleStatus {
  name: string
  enabled: boolean
}

// ── Moderation ────────────────────────────────────────────────────────────────
export interface Warning {
  id: string
  userId: string
  targetUsername?: string
  moderatorId: string
  reason: string
  createdAt: string
}

export interface ModLog {
  id: string
  action: string
  targetUserId: string
  targetUsername?: string
  moderatorId: string
  reason: string | null
  createdAt: string
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

// ── Logging ───────────────────────────────────────────────────────────────────
export interface LoggingConfig {
  messageLogChannelId: string | null
  memberLogChannelId: string | null
  voiceLogChannelId: string | null
  serverLogChannelId: string | null
}

// ── Welcome ───────────────────────────────────────────────────────────────────
export interface WelcomeConfig {
  welcomeEnabled: boolean
  welcomeChannelId: string | null
  welcomeMessage: string | null
  verifyChannelId: string | null
  verifyRoleId: string | null
}

// ── Security ──────────────────────────────────────────────────────────────────
export interface SecurityConfig {
  enabled: boolean
  messageThreshold: number
  messageWindowSeconds: number
  mentionThreshold: number
  antiInvite: boolean
  antiScamLink: boolean
  antiRaid: boolean
  raidThreshold: number
}
