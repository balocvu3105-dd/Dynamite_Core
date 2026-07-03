import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { loggingApi, guildsApi } from '@/api'
import { Card, Select, Button, Spinner } from '@/components/ui'
import { useState, useEffect } from 'react'
import { useToast } from '@/hooks/useToast'
import { useLangStore } from '@/i18n'
import type { LoggingConfig } from '@/types'

export default function LoggingPage() {
    const { guildId } = useParams<{ guildId: string }>()
    const qc = useQueryClient()
    const toast = useToast()
    const { t } = useLangStore()

    const logFields: { key: keyof LoggingConfig; label: string; desc: string }[] = [
        { key: 'messageLogChannelId', label: t.logging.msgLog, desc: t.logging.msgLogDesc },
        { key: 'memberLogChannelId', label: t.logging.memberLog, desc: t.logging.memberLogDesc },
        { key: 'voiceLogChannelId', label: t.logging.voiceLog, desc: t.logging.voiceLogDesc },
        { key: 'serverLogChannelId', label: t.logging.serverLog, desc: t.logging.serverLogDesc },
    ]

    const { data: config, isLoading: loadingConfig } = useQuery({
        queryKey: ['logging', guildId],
        queryFn: () => loggingApi.get(guildId!),
        enabled: !!guildId,
    })

    const { data: guildInfo, isLoading: loadingInfo } = useQuery({
        queryKey: ['guildInfo', guildId],
        queryFn: () => guildsApi.info(guildId!),
        enabled: !!guildId,
    })

    const [form, setForm] = useState<LoggingConfig>({
        messageLogChannelId: null,
        memberLogChannelId: null,
        voiceLogChannelId: null,
        serverLogChannelId: null,
    })

    useEffect(() => { if (config) setForm(config) }, [config])

    const { mutate: save, isPending } = useMutation({
        mutationFn: () => loggingApi.update(guildId!, form),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ['logging', guildId] })
            toast.success(t.common.savedSuccess)
        },
        onError: () => toast.error(t.common.error),
    })

    const textChannels = guildInfo?.channels.filter((c) => c.type === 'text') ?? []

    if (loadingConfig || loadingInfo) return <div className="flex justify-center py-20"><Spinner size="lg" /></div>

    return (
        <div className="max-w-2xl space-y-6">
            <div>
                <h2 className="text-lg font-semibold text-[--color-text]">{t.logging.title}</h2>
                <p className="text-sm text-[--color-text-muted] mt-1">
                    {t.logging.subtitle}
                </p>
            </div>

            <Card className="space-y-5">
                {logFields.map(({ key, label, desc }) => (
                    <div key={key}>
                        <Select
                            id={key}
                            label={label}
                            value={form[key] ?? ''}
                            onChange={(e) =>
                                setForm((f) => ({ ...f, [key]: e.target.value || null }))
                            }
                        >
                            <option value="" className="bg-[--color-surface-alt] text-[--color-text]">{t.common.selectChannel}</option>
                            {textChannels.map((ch) => (
                                <option key={ch.id} value={ch.id} className="bg-[--color-surface-alt] text-[--color-text]">#{ch.name}</option>
                            ))}
                        </Select>
                        <p className="mt-1 text-xs text-[--color-text-muted]">{desc}</p>
                    </div>
                ))}
            </Card>

            <Button onClick={() => save()} loading={isPending} className="cursor-pointer">{t.common.save}</Button>
        </div>
    )
}