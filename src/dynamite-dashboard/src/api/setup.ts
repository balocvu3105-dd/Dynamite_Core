import api from './client'

export interface SetupTemplateInfo {
  id: string
  name: string
  description: string
}

export interface SmartSetupRequest {
  topic: string
  scale: string
  enableEconomy: boolean
  enableTicket: boolean
  enableModeration: boolean
  enableVoice: boolean
}

export interface SetupPreview {
  name: string
  description: string
  roles: { name: string; color: string; hoisted: boolean }[]
  categories: {
    name: string
    channels: { name: string; type: string; topic?: string }[]
  }[]
}

export const setupApi = {
  getTemplates: (guildId: string) =>
    api.get<SetupTemplateInfo[]>(`/api/guilds/${guildId}/setup/templates`).then((r) => r.data),

  previewSmart: (guildId: string, data: SmartSetupRequest) =>
    api.post<SetupPreview>(`/api/guilds/${guildId}/setup/preview`, data).then((r) => r.data),
}
