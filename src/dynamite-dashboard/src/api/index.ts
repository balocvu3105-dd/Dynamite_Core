import api from './client'
import type {
  AuthResponse,
  GuildSummary,
  GuildInfo,
  ModuleStatus,
  PagedResult,
  Warning,
  ModLog,
  LoggingConfig,
  WelcomeConfig,
  SecurityConfig,
  ActivityLogsResponse,
} from '@/types'

// ── Auth ─────────────────────────────────────────────────────────────────────
export const authApi = {
  login: (code: string) =>
    api.post<AuthResponse>('/api/auth/discord', { code }).then((r) => r.data),
}

// ── Guilds ────────────────────────────────────────────────────────────────────
export const guildsApi = {
  list: () =>
    api.get<GuildSummary[]>('/api/guilds').then((r) => r.data),

  info: (guildId: string) =>
    api.get<GuildInfo>(`/api/guilds/${guildId}/info`).then((r) => r.data),

  getModules: (guildId: string) =>
    api.get<ModuleStatus[]>(`/api/guilds/${guildId}/modules`).then((r) => r.data),

  updateModule: (guildId: string, moduleName: string, enabled: boolean) =>
    api.patch(`/api/guilds/${guildId}/modules/${moduleName}`, { enabled }).then((r) => r.data),
}

// ── Moderation ────────────────────────────────────────────────────────────────
export const moderationApi = {
  getWarnings: (guildId: string, page = 1, pageSize = 20, userId?: string) =>
    api
      .get<PagedResult<Warning>>(`/api/guilds/${guildId}/moderation/warnings`, {
        params: { page, pageSize, userId },
      })
      .then((r) => r.data),

  deleteWarning: (guildId: string, warningId: string) =>
    api.delete(`/api/guilds/${guildId}/moderation/warnings/${warningId}`).then((r) => r.data),

  getModLogs: (guildId: string, page = 1, pageSize = 20, userId?: string) =>
    api
      .get<PagedResult<ModLog>>(`/api/guilds/${guildId}/moderation/modlogs`, {
        params: { page, pageSize, userId },
      })
      .then((r) => r.data),
}

// ── Logging ───────────────────────────────────────────────────────────────────
export const loggingApi = {
  get: (guildId: string) =>
    api.get<LoggingConfig>(`/api/guilds/${guildId}/logging`).then((r) => r.data),

  update: (guildId: string, data: Partial<LoggingConfig>) =>
    api.patch(`/api/guilds/${guildId}/logging`, data).then((r) => r.data),

  getActivities: (guildId: string, params?: { category?: number; search?: string; page?: number; pageSize?: number }) =>
    api.get<ActivityLogsResponse>(`/api/guilds/${guildId}/logging/activities`, { params }).then((r) => r.data),
}

// ── Welcome ───────────────────────────────────────────────────────────────────
export const welcomeApi = {
  get: (guildId: string) =>
    api.get<WelcomeConfig>(`/api/guilds/${guildId}/welcome`).then((r) => r.data),

  update: (guildId: string, data: Partial<WelcomeConfig>) =>
    api.patch(`/api/guilds/${guildId}/welcome`, data).then((r) => r.data),
}

// ── Security ──────────────────────────────────────────────────────────────────
export const securityApi = {
  get: (guildId: string) =>
    api.get<SecurityConfig>(`/api/guilds/${guildId}/security`).then((r) => r.data),

  update: (guildId: string, data: Partial<SecurityConfig>) =>
    api.patch(`/api/guilds/${guildId}/security`, data).then((r) => r.data),
}
