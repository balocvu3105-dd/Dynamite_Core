import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useQuery, useMutation } from '@tanstack/react-query'
import { Wand2, Shield, DollarSign, Ticket, Volume2, CheckCircle2, Layers } from 'lucide-react'
import { setupApi, type SmartSetupRequest } from '@/api/setup'
import { Card, Button, Spinner, Badge, Toggle } from '@/components/ui'
import { useToast } from '@/hooks/useToast'
import { useLangStore } from '@/i18n'

export default function SetupPage() {
  const { guildId } = useParams<{ guildId: string }>()
  const toast = useToast()
  const { t, lang } = useLangStore()

  const [topic, setTopic] = useState('Community')
  const [scale, setScale] = useState('Medium')
  const [enableEconomy, setEnableEconomy] = useState(true)
  const [enableTicket, setEnableTicket] = useState(true)
  const [enableModeration, setEnableModeration] = useState(true)
  const [enableVoice, setEnableVoice] = useState(true)

  const requestData: SmartSetupRequest = {
    topic,
    scale,
    enableEconomy,
    enableTicket,
    enableModeration,
    enableVoice,
  }

  const { data: templates, isLoading: loadingTemplates } = useQuery({
    queryKey: ['setup-templates', guildId],
    queryFn: () => setupApi.getTemplates(guildId!),
    enabled: !!guildId,
  })

  const {
    mutate: generatePreview,
    data: preview,
    isPending: isPreviewing,
  } = useMutation({
    mutationFn: () => setupApi.previewSmart(guildId!, requestData),
    onError: () => toast.error(lang === 'vi' ? 'Tạo sơ đồ cài đặt thất bại.' : 'Failed to generate smart setup plan.'),
  })

  const getLocalizedTemplate = (item: { id: string; name: string; description: string }) => {
    if (lang !== 'vi') return item
    if (item.id === 'gaming') {
      return {
        name: 'Hội Quán Game & Esports',
        description: 'Máy chủ cộng đồng game thủ với kênh tìm tổ đội (LFG), clip highlight và phòng thoại team.'
      }
    }
    if (item.id === 'community') {
      return {
        name: 'Cộng Đồng Tổng Hợp',
        description: 'Không gian giao lưu giải trí với các kênh thảo luận chung, meme, nghe nhạc và phòng trò chuyện.'
      }
    }
    if (item.id === 'streamer') {
      return {
        name: 'Streamer & Sáng Tạo Nội Dung',
        description: 'Thông báo livestream, góc fan art, thông báo video mới và phòng riêng cho Subscriber/Member.'
      }
    }
    return item
  }

  const getLocalizedPreview = (prv: { name: string; description: string }) => {
    if (lang !== 'vi') return prv
    return {
      name: prv.name.replace('Smart Setup', 'Sơ Đồ Thông Minh').replace('Esports & Gaming', 'Game & Esports').replace('Vibrant Community', 'Cộng Đồng Sôi Động').replace('Study & Research Club', 'CLB Học Tập').replace('Tech & Crypto Hub', 'Công Nghệ & Crypto'),
      description: `Sơ đồ cài đặt tối ưu cho cộng đồng quy mô ${scale === 'Small' ? 'Nhỏ' : scale === 'Medium' ? 'Trung bình' : 'Lớn'} theo chủ đề ${topic}.`
    }
  }

  return (
    <div className="max-w-4xl space-y-8">
      <div>
        <h2 className="text-xl font-bold text-[--color-text] flex items-center gap-2">
          <Wand2 className="text-[--color-brand]" /> {t.setup.title}
        </h2>
        <p className="text-sm text-[--color-text-muted] mt-1">
          {t.setup.subtitle}
        </p>
      </div>

      {/* Preset Templates */}
      <div className="space-y-4">
        <h3 className="text-md font-semibold text-[--color-text] flex items-center gap-2">
          <Layers size={18} /> {lang === 'vi' ? 'Các Mẫu Chuyên Gia Có Sẵn' : 'Instant Preset Templates'}
        </h3>
        {loadingTemplates ? (
          <Spinner />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {templates?.map((item) => {
              const loc = getLocalizedTemplate(item)
              return (
                <Card key={item.id} className="flex flex-col justify-between border-[--color-border] hover:border-[--color-brand] transition-colors">
                  <div>
                    <h4 className="font-semibold text-[--color-text]">{loc.name}</h4>
                    <p className="text-xs text-[--color-text-muted] mt-2">{loc.description}</p>
                  </div>
                  <div className="mt-4 pt-3 border-t border-[--color-border] flex justify-end">
                    <Badge variant="success">
                      {lang === 'vi' ? `Có sẵn qua /setup ${item.id}` : `Available via /setup ${item.id}`}
                    </Badge>
                  </div>
                </Card>
              )
            })}
          </div>
        )}
      </div>

      {/* Smart Setup Wizard */}
      <Card className="space-y-6 bg-[--color-surface] border-2 border-[--color-brand]/30">
        <div>
          <h3 className="text-lg font-semibold text-[--color-text]">{t.setup.customEngine}</h3>
          <p className="text-xs text-[--color-text-muted]">
            {t.setup.customDesc}
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label className="block text-xs font-medium text-[--color-text-muted] uppercase tracking-wider mb-2">
              {t.setup.topicLabel}
            </label>
            <select
              value={topic}
              onChange={(e) => setTopic(e.target.value)}
              className="w-full bg-[--color-surface-alt] border border-[--color-border] rounded-md px-3 py-2 text-sm text-[--color-text] focus:ring-2 focus:ring-[--color-brand] outline-none"
            >
              <option value="Community" className="bg-[--color-surface-alt] text-[--color-text]">{t.setup.topics.general}</option>
              <option value="Gaming" className="bg-[--color-surface-alt] text-[--color-text]">{t.setup.topics.gaming}</option>
              <option value="Study" className="bg-[--color-surface-alt] text-[--color-text]">{t.setup.topics.study}</option>
              <option value="CryptoOrTech" className="bg-[--color-surface-alt] text-[--color-text]">{t.setup.topics.tech}</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-medium text-[--color-text-muted] uppercase tracking-wider mb-2">
              {t.setup.scaleLabel}
            </label>
            <select
              value={scale}
              onChange={(e) => setScale(e.target.value)}
              className="w-full bg-[--color-surface-alt] border border-[--color-border] rounded-md px-3 py-2 text-sm text-[--color-text] focus:ring-2 focus:ring-[--color-brand] outline-none"
            >
              <option value="Small" className="bg-[--color-surface-alt] text-[--color-text]">{t.setup.scales.small}</option>
              <option value="Medium" className="bg-[--color-surface-alt] text-[--color-text]">{t.setup.scales.medium}</option>
              <option value="Large" className="bg-[--color-surface-alt] text-[--color-text]">{t.setup.scales.large}</option>
            </select>
          </div>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 pt-2">
          <div className="flex items-center justify-between p-3 rounded-lg bg-[--color-surface-alt]">
            <span className="text-xs font-medium flex items-center gap-1.5 text-[--color-text]">
              <DollarSign size={15} className="text-emerald-400" /> {t.setup.toggles.economy}
            </span>
            <Toggle checked={enableEconomy} onChange={setEnableEconomy} />
          </div>

          <div className="flex items-center justify-between p-3 rounded-lg bg-[--color-surface-alt]">
            <span className="text-xs font-medium flex items-center gap-1.5 text-[--color-text]">
              <Ticket size={15} className="text-blue-400" /> {t.setup.toggles.tickets}
            </span>
            <Toggle checked={enableTicket} onChange={setEnableTicket} />
          </div>

          <div className="flex items-center justify-between p-3 rounded-lg bg-[--color-surface-alt]">
            <span className="text-xs font-medium flex items-center gap-1.5 text-[--color-text]">
              <Shield size={15} className="text-amber-400" /> {t.setup.toggles.staffMod}
            </span>
            <Toggle checked={enableModeration} onChange={setEnableModeration} />
          </div>

          <div className="flex items-center justify-between p-3 rounded-lg bg-[--color-surface-alt]">
            <span className="text-xs font-medium flex items-center gap-1.5 text-[--color-text]">
              <Volume2 size={15} className="text-purple-400" /> {t.setup.toggles.voiceRooms}
            </span>
            <Toggle checked={enableVoice} onChange={setEnableVoice} />
          </div>
        </div>

        <div className="pt-2 flex justify-end">
          <Button onClick={() => generatePreview()} loading={isPreviewing} className="cursor-pointer">
            <Wand2 size={16} /> {isPreviewing ? t.setup.generating : t.setup.generateBtn}
          </Button>
        </div>
      </Card>

      {/* Preview Output */}
      {preview && (() => {
        const locPrv = getLocalizedPreview(preview)
        return (
          <Card className="space-y-6 border-emerald-500/50 bg-emerald-950/10">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-bold text-lg text-emerald-400 flex items-center gap-2">
                  <CheckCircle2 size={20} /> {t.setup.blueprintTitle} {locPrv.name}
                </h4>
                <p className="text-xs text-[--color-text-muted]">{locPrv.description}</p>
              </div>
              <Badge variant="success">{t.setup.readyDeploy}</Badge>
            </div>

            <div>
              <h5 className="text-xs font-semibold uppercase tracking-wider text-[--color-text-muted] mb-2">
                {t.setup.rolesToCreate} ({preview.roles.length})
              </h5>
              <div className="flex flex-wrap gap-2">
                {preview.roles.map((r, i) => (
                  <span
                    key={i}
                    className="px-2.5 py-1 rounded text-xs font-semibold bg-[--color-surface-alt] border border-[--color-border]"
                    style={{ color: r.color !== '#000000' ? r.color : 'inherit' }}
                  >
                    @ {r.name}
                  </span>
                ))}
              </div>
            </div>

            <div className="space-y-4">
              <h5 className="text-xs font-semibold uppercase tracking-wider text-[--color-text-muted]">
                {t.setup.channelLayout} ({preview.categories.reduce((acc, c) => acc + c.channels.length, 0)} {t.common.channel})
              </h5>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {preview.categories.map((cat, idx) => (
                  <div key={idx} className="p-3 rounded-lg bg-[--color-surface-alt] border border-[--color-border]">
                    <div className="font-bold text-xs text-[--color-brand] tracking-wide mb-2">
                      {cat.name}
                    </div>
                    <ul className="space-y-1.5">
                      {cat.channels.map((ch, cIdx) => (
                        <li key={cIdx} className="text-xs text-[--color-text] flex items-center justify-between">
                          <span>
                            {ch.type === 'Voice' ? '🔊' : '💬'} #{ch.name}
                          </span>
                          {ch.topic && <span className="text-[10px] text-[--color-text-muted] truncate max-w-[150px]">{ch.topic}</span>}
                        </li>
                      ))}
                    </ul>
                  </div>
                ))}
              </div>
            </div>

            <div className="pt-4 border-t border-[--color-border] flex justify-between items-center text-xs text-[--color-text-muted]">
              <span>{t.setup.deployHint}</span>
            </div>
          </Card>
        )
      })()}
    </div>
  )
}
