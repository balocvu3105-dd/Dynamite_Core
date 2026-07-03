import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { securityApi } from '@/api'
import { Card, Toggle, Input, Button, Spinner } from '@/components/ui'
import { useState, useEffect } from 'react'
import { useToast } from '@/hooks/useToast'
import { useLangStore } from '@/i18n'
import type { SecurityConfig } from '@/types'

export default function SecurityPage() {
    const { guildId } = useParams<{ guildId: string }>()
    const qc = useQueryClient()
    const toast = useToast()
    const { t, lang } = useLangStore()

    const featureToggles: { key: keyof SecurityConfig; label: string; desc: string }[] = [
        { key: 'antiInvite', label: t.security.antiInvite, desc: t.security.antiInviteDesc },
        { key: 'antiScamLink', label: t.security.antiScam, desc: t.security.antiScamDesc },
        { key: 'antiRaid', label: t.security.antiRaid, desc: t.security.antiRaidDesc },
    ]

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
            toast.success(t.common.savedSuccess)
        },
        onError: () => toast.error(t.common.error),
    })

    if (isLoading) return <div className="flex justify-center py-20"><Spinner size="lg" /></div>

    return (
        <div className="max-w-2xl space-y-6">
            <div>
                <h2 className="text-lg font-semibold text-[--color-text]">{t.security.title}</h2>
                <p className="text-sm text-[--color-text-muted] mt-1">
                    {t.security.subtitle}
                </p>
            </div>

            {/* Master toggle */}
            <Card className="flex items-center justify-between">
                <div>
                    <p className="text-sm font-medium text-[--color-text]">{t.security.enableSecurity}</p>
                    <p className="text-xs text-[--color-text-muted] mt-0.5">
                        {lang === 'vi' ? 'Kích hoạt toàn bộ các luật bảo vệ đã cài đặt bên dưới' : 'Activates all configured security rules'}
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
                        <p className="text-sm font-medium text-[--color-text]">{t.security.antiSpam}</p>
                        <div className="grid grid-cols-2 gap-4">
                            <Input
                                id="messageThreshold"
                                label={t.security.msgThreshold}
                                type="number"
                                min={2} max={30}
                                value={form.messageThreshold}
                                onChange={(e) => setForm((f) => ({ ...f, messageThreshold: +e.target.value }))}
                            />
                            <Input
                                id="messageWindowSeconds"
                                label={t.security.msgWindow}
                                type="number"
                                min={1} max={30}
                                value={form.messageWindowSeconds}
                                onChange={(e) => setForm((f) => ({ ...f, messageWindowSeconds: +e.target.value }))}
                            />
                        </div>
                        <Input
                            id="mentionThreshold"
                            label={lang === 'vi' ? 'Giới hạn nhắc tên (@mention) tối đa' : 'Max mentions per message'}
                            type="number"
                            min={1} max={20}
                            value={form.mentionThreshold}
                            onChange={(e) => setForm((f) => ({ ...f, mentionThreshold: +e.target.value }))}
                        />
                    </Card>

                    {/* Feature toggles */}
                    <Card className="space-y-4">
                        {featureToggles.map(({ key, label, desc }) => (
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
                                label={t.security.raidThreshold}
                                type="number"
                                min={3} max={50}
                                value={form.raidThreshold}
                                onChange={(e) => setForm((f) => ({ ...f, raidThreshold: +e.target.value }))}
                            />
                        </Card>
                    )}
                </>
            )}

            <Button onClick={() => save()} loading={isPending} className="cursor-pointer">{t.common.save}</Button>
        </div>
    )
}