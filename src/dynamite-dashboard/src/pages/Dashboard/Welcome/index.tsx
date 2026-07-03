import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { welcomeApi, guildsApi } from '@/api'
import { Card, Select, Input, Button, Toggle, Spinner } from '@/components/ui'
import { useState, useEffect } from 'react'
import { useToast } from '@/hooks/useToast'
import { useLangStore } from '@/i18n'
import type { WelcomeConfig } from '@/types'

export default function WelcomePage() {
    const { guildId } = useParams<{ guildId: string }>()
    const qc = useQueryClient()
    const toast = useToast()
    const { t, lang } = useLangStore()

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
            toast.success(t.common.savedSuccess)
        },
        onError: () => toast.error(t.common.error),
    })

    const textChannels = guildInfo?.channels.filter((c) => c.type === 'text') ?? []
    const roles = guildInfo?.roles.filter((r) => !r.isManaged) ?? []

    const previewMsg = (form.welcomeMessage || t.welcome.welcomeMsgPlaceholder)
        .replace(/{user}/g, '✨ @NewMember ✨')
        .replace(/{username}/g, 'NewMember')
        .replace(/{server}/g, guildInfo?.name || 'My Discord Server')

    if (loadingConfig || loadingInfo) return <div className="flex justify-center py-20"><Spinner size="lg" /></div>

    return (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8">
            {/* Left Column: Form Settings */}
            <div className="lg:col-span-7 space-y-6">
                <div>
                    <h2 className="text-lg font-semibold text-[--color-text]">{t.welcome.title}</h2>
                    <p className="text-sm text-[--color-text-muted] mt-1">
                        {t.welcome.subtitle}
                    </p>
                </div>

                {/* Welcome section */}
                <Card className="space-y-5 border-l-4 border-l-[--color-brand]">
                    <div className="flex items-center justify-between">
                        <div>
                            <p className="text-sm font-medium text-[--color-text]">{t.welcome.enableWelcome}</p>
                            <p className="text-xs text-[--color-text-muted] mt-0.5">{lang === 'vi' ? 'Tự động gửi thông báo khi có thành viên mới gia nhập' : 'Send a message when members join'}</p>
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
                                label={t.welcome.welcomeChannel}
                                value={form.welcomeChannelId ?? ''}
                                onChange={(e) =>
                                    setForm((f) => ({ ...f, welcomeChannelId: e.target.value || null }))
                                }
                            >
                                <option value="" className="bg-[--color-surface-alt] text-[--color-text]">{t.common.selectChannel}</option>
                                {textChannels.map((ch) => (
                                    <option key={ch.id} value={ch.id} className="bg-[--color-surface-alt] text-[--color-text]">#{ch.name}</option>
                                ))}
                            </Select>

                            <Input
                                id="welcomeMessage"
                                label={t.welcome.welcomeMsg}
                                placeholder={t.welcome.welcomeMsgPlaceholder}
                                value={form.welcomeMessage ?? ''}
                                onChange={(e) =>
                                    setForm((f) => ({ ...f, welcomeMessage: e.target.value || null }))
                                }
                            />
                            <p className="text-xs text-[--color-text-muted]">
                                {lang === 'vi' ? 'Các biến:' : 'Variables:'} <code className="text-[--color-brand]">{'{user}'}</code> — mention,{' '}
                                <code className="text-[--color-brand]">{'{username}'}</code> — name,{' '}
                                <code className="text-[--color-brand]">{'{server}'}</code> — server name
                            </p>
                        </>
                    )}
                </Card>

                {/* Verification section */}
                <Card className="space-y-5">
                    <div>
                        <p className="text-sm font-medium text-[--color-text]">{lang === 'vi' ? 'Hệ thống Xác thực (Verification Gate)' : 'Verification'}</p>
                        <p className="text-xs text-[--color-text-muted] mt-0.5">
                            {lang === 'vi' ? 'Vai trò tự động được cấp khi thành viên bấm nút xác thực' : 'Role assigned when a member clicks the verify button'}
                        </p>
                    </div>

                    <Select
                        id="verifyChannel"
                        label={t.welcome.verifyChannel}
                        value={form.verifyChannelId ?? ''}
                        onChange={(e) =>
                            setForm((f) => ({ ...f, verifyChannelId: e.target.value || null }))
                        }
                    >
                        <option value="" className="bg-[--color-surface-alt] text-[--color-text]">{t.common.selectChannel}</option>
                        {textChannels.map((ch) => (
                            <option key={ch.id} value={ch.id} className="bg-[--color-surface-alt] text-[--color-text]">#{ch.name}</option>
                        ))}
                    </Select>

                    <Select
                        id="verifyRole"
                        label={t.welcome.verifyRole}
                        value={form.verifyRoleId ?? ''}
                        onChange={(e) =>
                            setForm((f) => ({ ...f, verifyRoleId: e.target.value || null }))
                        }
                    >
                        <option value="" className="bg-[--color-surface-alt] text-[--color-text]">{t.common.selectRole}</option>
                        {roles.map((r) => (
                            <option key={r.id} value={r.id} className="bg-[--color-surface-alt] text-[--color-text]">{r.name}</option>
                        ))}
                    </Select>
                </Card>

                <Button onClick={() => save()} loading={isPending} className="cursor-pointer">{t.common.save}</Button>
            </div>

            {/* Right Column: Live Discord Chat Preview */}
            <div className="lg:col-span-5 space-y-4">
                <div className="sticky top-20">
                    <h3 className="text-sm font-semibold uppercase tracking-wider text-[--color-text-muted] mb-3 flex items-center gap-2">
                        <span>💬</span> {lang === 'vi' ? 'Xem Trước Trực Tiếp Trên Discord' : 'Live Discord Preview'}
                    </h3>
                    
                    <div className="rounded-xl overflow-hidden border border-[--color-border] bg-[#313338] shadow-2xl">
                        {/* Fake Discord Channel Header */}
                        <div className="h-10 bg-[#2b2d31] px-4 flex items-center gap-2 border-b border-[#1f2023] text-sm font-semibold text-gray-200">
                            <span className="text-gray-400">#</span>
                            <span>{textChannels.find(c => c.id === form.welcomeChannelId)?.name || 'welcome-gate'}</span>
                        </div>

                        {/* Fake Chat Message Area */}
                        <div className="p-4 space-y-4 font-sans text-sm">
                            <div className="flex gap-3 items-start hover:bg-[#2e3035] p-2 rounded transition-colors">
                                <div className="w-10 h-10 rounded-full bg-[--color-brand] flex items-center justify-center text-white font-bold shrink-0 shadow-md">
                                    D🚀
                                </div>
                                <div className="space-y-1">
                                    <div className="flex items-baseline gap-2">
                                        <span className="font-semibold text-pink-400">Dynamite Bot</span>
                                        <span className="px-1.5 py-0.5 rounded bg-[#5865f2] text-white text-[10px] font-bold">BOT</span>
                                        <span className="text-xs text-gray-400">Hôm nay lúc 12:00</span>
                                    </div>
                                    <div className="text-gray-200 leading-relaxed break-words whitespace-pre-wrap">
                                        {previewMsg}
                                    </div>
                                    <div className="mt-2 p-3 rounded-lg bg-[#2b2d31] border-l-4 border-l-[--color-brand] max-w-sm">
                                        <div className="text-xs font-bold text-pink-400 mb-1">🎉 {lang === 'vi' ? 'Thành viên mới gia nhập!' : 'New Member Joined!'}</div>
                                        <div className="text-xs text-gray-300">
                                            {lang === 'vi' ? 'Chào mừng bạn đến với' : 'Welcome to'} <strong className="text-white">{guildInfo?.name || 'Server'}</strong>. {lang === 'vi' ? 'Bạn là thành viên thứ #100!' : 'You are member #100!'}
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <p className="text-xs text-[--color-text-muted] mt-2 italic text-center">
                        {lang === 'vi' ? '⚡ Thay đổi nội dung bên trái để xem kết quả mô phỏng tức thì.' : '⚡ Type on the left to see live simulated output.'}
                    </p>
                </div>
            </div>
        </div>
    )
}