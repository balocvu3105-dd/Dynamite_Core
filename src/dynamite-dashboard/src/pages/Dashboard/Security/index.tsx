import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { securityApi } from '@/api'
import { Card, Toggle, Input, Button, Spinner } from '@/components/ui'
import { useState, useEffect } from 'react'
import type { SecurityConfig } from '@/types'

export default function SecurityPage() {
    const { guildId } = useParams<{ guildId: string }>()
    const qc = useQueryClient()

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
        onSuccess: () => qc.invalidateQueries({ queryKey: ['security', guildId] }),
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
            <Card>
                <div className="flex items-center justify-between">
                    <div>
                        <p className="text-sm font-medium text-[--color-text]">Anti-Spam System</p>
                        <p className="text-xs text-[--color-text-muted] mt-0.5">Enable all security features</p>
                    </div>
                    <Toggle
                        checked={form.enabled}
                        onChange={(v) => setForm((f) => ({ ...f, enabled: v }))}
                    />
                </div>
            </Card>

            {form.enabled && (
                <>
                    {/* Spam thresholds */}
                    <Card className="space-y-5">
                        <p className="text-sm font-semibold text-[--color-text]">Spam Detection</p>
                        <div className="grid grid-cols-2 gap-4">
                            <Input
                                id="msgThreshold"
                                label="Max messages"
                                type="number"
                                min={2} max={30}
                                value={form.messageThreshold}
                                onChange={(e) => setForm((f) => ({ ...f, messageThreshold: +e.target.value }))}
                            />
                            <Input
                                id="msgWindow"
                                label="Time window (seconds)"
                                type="number"
                                min={2} max={60}
                                value={form.messageWindowSeconds}
                                onChange={(e) => setForm((f) => ({ ...f, messageWindowSeconds: +e.target.value }))}
                            />
                        </div>
                        <Input
                            id="mentionThreshold"
                            label="Max mentions per message"
                            type="number"
                            min={2} max={20}
                            value={form.mentionThreshold}
                            onChange={(e) => setForm((f) => ({ ...f, mentionThreshold: +e.target.value }))}
                        />
                    </Card>

                    {/* Feature toggles */}
                    <Card className="space-y-4">
                        <p className="text-sm font-semibold text-[--color-text]">Protection Features</p>
                        {([
                            { key: 'antiInvite' as const, label: 'Anti-Invite', desc: 'Block Discord invite links' },
                            { key: 'antiScamLink' as const, label: 'Anti-Scam Link', desc: 'Block known scam URLs' },
                            { key: 'antiRaid' as const, label: 'Anti-Raid', desc: 'Lockdown on mass joins' },
                        ] as const).map(({ key, label, desc }) => (
                            <div key={key} className="flex items-center justify-between">
                                <div>
                                    <p className="text-sm text-[--color-text]">{label}</p>
                                    <p className="text-xs text-[--color-text-muted]">{desc}</p>
                                </div>
                                <Toggle
                                    checked={form[key]}
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