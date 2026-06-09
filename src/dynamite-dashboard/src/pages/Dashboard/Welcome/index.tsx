import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { welcomeApi, guildsApi } from '@/api'
import { Card, Select, Input, Button, Toggle, Spinner } from '@/components/ui'
import { useState, useEffect } from 'react'
import { useToast } from '@/hooks/useToast'
import type { WelcomeConfig } from '@/types'

export default function WelcomePage() {
    const { guildId } = useParams<{ guildId: string }>()
    const qc = useQueryClient()
    const toast = useToast()

    const { data: config, isLoading: loadingConfig } = useQuery({
        queryKey: ['welcome', guildId],
        queryFn: () => welcomeApi.get(guildId!),
        enabled: !!guildId,
    })

    const { data: guildInfo, isLoading: loadingInfo } = useQuery({
        queryKey: ['guildInfo', guildId],
        queryFn: () => guildsApi.info(guildId!),
        enabled: !!guildId,
    })

    const [form, setForm] = useState<WelcomeConfig>({
        welcomeEnabled: false,
        welcomeChannelId: null,
        welcomeMessage: null,
        verifyChannelId: null,
        verifyRoleId: null,
    })

    useEffect(() => { if (config) setForm(config) }, [config])

    const { mutate: save, isPending } = useMutation({
        mutationFn: () => welcomeApi.update(guildId!, form),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ['welcome', guildId] })
            toast.success('Welcome settings saved.')
        },
        onError: () => toast.error('Failed to save welcome settings.'),
    })

    const textChannels = guildInfo?.channels.filter((c) => c.type === 'text') ?? []
    const roles = guildInfo?.roles.filter((r) => !r.isManaged) ?? []

    if (loadingConfig || loadingInfo) return <div className="flex justify-center py-20"><Spinner size="lg" /></div>

    return (
        <div className="max-w-2xl space-y-6">
            <div>
                <h2 className="text-lg font-semibold text-[--color-text]">Welcome & Verification</h2>
                <p className="text-sm text-[--color-text-muted] mt-1">
                    Configure welcome messages and member verification.
                </p>
            </div>

            {/* Welcome section */}
            <Card className="space-y-5">
                <div className="flex items-center justify-between">
                    <div>
                        <p className="text-sm font-medium text-[--color-text]">Welcome messages</p>
                        <p className="text-xs text-[--color-text-muted] mt-0.5">Send a message when members join</p>
                    </div>
                    <Toggle
                        checked={form.welcomeEnabled}
                        onChange={(v) => setForm((f) => ({ ...f, welcomeEnabled: v }))}
                    />
                </div>

                {form.welcomeEnabled && (
                    <>
                        <Select
                            id="welcomeChannel"
                            label="Welcome channel"
                            value={form.welcomeChannelId ?? ''}
                            onChange={(e) =>
                                setForm((f) => ({ ...f, welcomeChannelId: e.target.value || null }))
                            }
                        >
                            <option value="">— Not set —</option>
                            {textChannels.map((ch) => (
                                <option key={ch.id} value={ch.id}>#{ch.name}</option>
                            ))}
                        </Select>

                        <Input
                            id="welcomeMessage"
                            label="Welcome message"
                            placeholder="Welcome {user} to {server}!"
                            value={form.welcomeMessage ?? ''}
                            onChange={(e) =>
                                setForm((f) => ({ ...f, welcomeMessage: e.target.value || null }))
                            }
                        />
                        <p className="text-xs text-[--color-text-muted]">
                            Variables: <code className="text-[--color-brand]">{'{user}'}</code> — mention,{' '}
                            <code className="text-[--color-brand]">{'{username}'}</code> — name,{' '}
                            <code className="text-[--color-brand]">{'{server}'}</code> — server name
                        </p>
                    </>
                )}
            </Card>

            {/* Verification section */}
            <Card className="space-y-5">
                <div>
                    <p className="text-sm font-medium text-[--color-text]">Verification</p>
                    <p className="text-xs text-[--color-text-muted] mt-0.5">
                        Role assigned when a member clicks the verify button
                    </p>
                </div>

                <Select
                    id="verifyChannel"
                    label="Verify panel channel"
                    value={form.verifyChannelId ?? ''}
                    onChange={(e) =>
                        setForm((f) => ({ ...f, verifyChannelId: e.target.value || null }))
                    }
                >
                    <option value="">— Not set —</option>
                    {textChannels.map((ch) => (
                        <option key={ch.id} value={ch.id}>#{ch.name}</option>
                    ))}
                </Select>

                <Select
                    id="verifyRole"
                    label="Verified role"
                    value={form.verifyRoleId ?? ''}
                    onChange={(e) =>
                        setForm((f) => ({ ...f, verifyRoleId: e.target.value || null }))
                    }
                >
                    <option value="">— Not set —</option>
                    {roles.map((r) => (
                        <option key={r.id} value={r.id}>{r.name}</option>
                    ))}
                </Select>
            </Card>

            <Button onClick={() => save()} loading={isPending}>Save changes</Button>
        </div>
    )
}