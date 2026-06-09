import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { securityApi } from '@/api'
import { Card, Toggle, Input, Button, Spinner } from '@/components/ui'
import { useState, useEffect } from 'react'
import { useToast } from '@/hooks/useToast'
import type { SecurityConfig } from '@/types'

const FEATURE_TOGGLES: { key: keyof SecurityConfig; label: string; desc: string }[] = [
    { key: 'antiInvite', label: 'Anti Invite', desc: 'Block Discord server invite links' },
    { key: 'antiScamLink', label: 'Anti Scam Link', desc: 'Detect and remove known scam URLs' },
    { key: 'antiRaid', label: 'Anti Raid', desc: 'Slow or lock server during join spikes' },
]

export default function SecurityPage() {
    const { guildId } = useParams<{ guildId: string }>()
    const qc = useQueryClient()
    const toast = useToast()

    const { data: config, isLoading } = useQuery({
        queryKey: ['security', guildId],
        queryFn: () => securityApi.get(guildId!),
        enabled: !!guildId,
    })

    const [form, setForm] = useState<SecurityConfig>({
        enabled: false,
        messageThreshold: 5,
        messageWindowSeconds: 5,
        mentionThreshold: 5,
        antiInvite: false,
        antiScamLink: false,
        antiRaid: false,
        raidThreshold: 10,
    })

    useEffect(() => { if (config) setForm(config) }, [config])

    const { mutate: save, isPending } = useMutation({
        mutationFn: () => securityApi.update(guildId!, form),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ['security', guildId] })
            toast.success('Security settings saved.')
        },
        onError: () => toast.error('Failed to save security settings.'),
    })

    if (isLoading) return <div className="flex justify-center py-20"><Spinner size="lg" /></div>

    return (
        <div className="max-w-2xl space-y-6">
            <div>
                <h2 className="text-lg font-semibold text-[--color-text]">Security & Anti-Spam</h2>
                <p className="text-sm text-[--color-text-muted] mt-1">
                    Protect your server from spam, raids, and malicious content.
                </p>
            </div>

            {/* Master toggle */}
            <Card className="flex items-center justify-between">
                <div>
                    <p className="text-sm font-medium text-[--color-text]">Enable security module</p>
                    <p className="text-xs text-[--color-text-muted] mt-0.5">
                        Activates all configured security rules
                    </p>
                </div>
                <Toggle
                    checked={form.enabled}
                    onChange={(v) => setForm((f) => ({ ...f, enabled: v }))}
                />
            </Card>

            {form.enabled && (
                <>
                    {/* Spam thresholds */}
                    <Card className="space-y-5">
                        <p className="text-sm font-medium text-[--color-text]">Spam thresholds</p>
                        <div className="grid grid-cols-2 gap-4">
                            <Input
                                id="messageThreshold"
                                label="Messages per window"
                                type="number"
                                min={2} max={30}
                                value={form.messageThreshold}
                                onChange={(e) => setForm((f) => ({ ...f, messageThreshold: +e.target.value }))}
                            />
                            <Input
                                id="messageWindowSeconds"
                                label="Window (seconds)"
                                type="number"
                                min={1} max={30}
                                value={form.messageWindowSeconds}
                                onChange={(e) => setForm((f) => ({ ...f, messageWindowSeconds: +e.target.value }))}
                            />
                        </div>
                        <Input
                            id="mentionThreshold"
                            label="Max mentions per message"
                            type="number"
                            min={1} max={20}
                            value={form.mentionThreshold}
                            onChange={(e) => setForm((f) => ({ ...f, mentionThreshold: +e.target.value }))}
                        />
                    </Card>

                    {/* Feature toggles */}
                    <Card className="space-y-4">
                        {FEATURE_TOGGLES.map(({ key, label, desc }) => (
                            <div key={key} className="flex items-center justify-between">
                                <div>
                                    <p className="text-sm text-[--color-text]">{label}</p>
                                    <p className="text-xs text-[--color-text-muted]">{desc}</p>
                                </div>
                                <Toggle
                                    checked={form[key] as boolean}
                                    onChange={(v) => setForm((f) => ({ ...f, [key]: v }))}
                                />
                            </div>
                        ))}
                    </Card>

                    {/* Raid threshold */}
                    {form.antiRaid && (
                        <Card>
                            <Input
                                id="raidThreshold"
                                label="Raid threshold (joins per 10s)"
                                type="number"
                                min={3} max={50}
                                value={form.raidThreshold}
                                onChange={(e) => setForm((f) => ({ ...f, raidThreshold: +e.target.value }))}
                            />
                        </Card>
                    )}
                </>
            )}

            <Button onClick={() => save()} loading={isPending}>Save changes</Button>
        </div>
    )
}