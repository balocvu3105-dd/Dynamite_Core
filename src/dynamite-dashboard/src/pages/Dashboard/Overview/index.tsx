import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { Shield, MessageSquare, FileText, Lock, Bot } from 'lucide-react'
import { guildsApi } from '@/api'
import { Card, Toggle, Spinner, Badge } from '@/components/ui'
import type { ModuleStatus } from '@/types'

const MODULE_META: Record<string, { label: string; description: string; icon: React.ElementType }> = {
    moderation: { label: 'Moderation', description: 'Ban, kick, warn, timeout commands', icon: Shield },
    welcome: { label: 'Welcome', description: 'Welcome messages and verification', icon: MessageSquare },
    logging: { label: 'Logging', description: 'Track message edits, member changes', icon: FileText },
    autorole: { label: 'Auto Role', description: 'Assign roles automatically on join', icon: Bot },
    security: { label: 'Security', description: 'Anti-spam, anti-raid protection', icon: Lock },
}

export default function OverviewPage() {
    const { guildId } = useParams<{ guildId: string }>()
    const qc = useQueryClient()

    const { data: modules, isLoading } = useQuery({
        queryKey: ['modules', guildId],
        queryFn: () => guildsApi.getModules(guildId!),
        enabled: !!guildId,
    })

    const { mutate: toggleModule, isPending } = useMutation({
        mutationFn: ({ name, enabled }: { name: string; enabled: boolean }) =>
            guildsApi.updateModule(guildId!, name, enabled),
        onSuccess: () => qc.invalidateQueries({ queryKey: ['modules', guildId] }),
    })

    if (isLoading) return <div className="flex justify-center py-20"><Spinner size="lg" /></div>

    return (
        <div className="max-w-3xl space-y-6">
            <div>
                <h2 className="text-lg font-semibold text-[--color-text]">Modules</h2>
                <p className="text-sm text-[--color-text-muted] mt-1">
                    Enable or disable features for this server.
                </p>
            </div>

            <div className="space-y-3">
                {modules?.map((mod: ModuleStatus) => {
                    const meta = MODULE_META[mod.name]
                    if (!meta) return null
                    const Icon = meta.icon
                    return (
                        <Card key={mod.name} className="flex items-center gap-4">
                            <div className="w-9 h-9 rounded-lg bg-[--color-surface-raised] flex items-center justify-center flex-shrink-0">
                                <Icon size={18} className="text-[--color-text-muted]" />
                            </div>
                            <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-2">
                                    <p className="text-sm font-medium text-[--color-text]">{meta.label}</p>
                                    <Badge variant={mod.enabled ? 'success' : 'default'}>
                                        {mod.enabled ? 'On' : 'Off'}
                                    </Badge>
                                </div>
                                <p className="text-xs text-[--color-text-muted] mt-0.5">{meta.description}</p>
                            </div>
                            <Toggle
                                checked={mod.enabled}
                                disabled={isPending}
                                onChange={(enabled) => toggleModule({ name: mod.name, enabled })}
                            />
                        </Card>
                    )
                })}
            </div>
        </div>
    )
}