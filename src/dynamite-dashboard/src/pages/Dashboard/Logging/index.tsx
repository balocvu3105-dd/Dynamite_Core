import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { loggingApi, guildsApi } from '@/api'
import { Card, Select, Button, Spinner } from '@/components/ui'
import { useState, useEffect } from 'react'
import { useToast } from '@/hooks/useToast'
import type { LoggingConfig } from '@/types'

const LOG_FIELDS: { key: keyof LoggingConfig; label: string; desc: string }[] = [
    { key: 'messageLogChannelId', label: 'Message Logs', desc: 'Deleted and edited messages' },
    { key: 'memberLogChannelId', label: 'Member Logs', desc: 'Joins, leaves, role changes' },
    { key: 'voiceLogChannelId', label: 'Voice Logs', desc: 'Voice channel activity' },
    { key: 'serverLogChannelId', label: 'Server Logs', desc: 'Channel and role changes' },
]

export default function LoggingPage() {
    const { guildId } = useParams<{ guildId: string }>()
    const qc = useQueryClient()
    const toast = useToast()

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
            toast.success('Logging channels saved.')
        },
        onError: () => toast.error('Failed to save logging channels.'),
    })

    const textChannels = guildInfo?.channels.filter((c) => c.type === 'text') ?? []

    if (loadingConfig || loadingInfo) return <div className="flex justify-center py-20"><Spinner size="lg" /></div>

    return (
        <div className="max-w-2xl space-y-6">
            <div>
                <h2 className="text-lg font-semibold text-[--color-text]">Logging Channels</h2>
                <p className="text-sm text-[--color-text-muted] mt-1">
                    Set which channel each log category is sent to.
                </p>
            </div>

            <Card className="space-y-5">
                {LOG_FIELDS.map(({ key, label, desc }) => (
                    <div key={key}>
                        <Select
                            id={key}
                            label={label}
                            value={form[key] ?? ''}
                            onChange={(e) =>
                                setForm((f) => ({ ...f, [key]: e.target.value || null }))
                            }
                        >
                            <option value="">— Not set —</option>
                            {textChannels.map((ch) => (
                                <option key={ch.id} value={ch.id}>#{ch.name}</option>
                            ))}
                        </Select>
                        <p className="mt-1 text-xs text-[--color-text-muted]">{desc}</p>
                    </div>
                ))}
            </Card>

            <Button onClick={() => save()} loading={isPending}>Save changes</Button>
        </div>
    )
}