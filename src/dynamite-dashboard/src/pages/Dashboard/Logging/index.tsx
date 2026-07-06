import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { loggingApi, guildsApi } from '@/api'
import { Card, Select, Button, Spinner, Badge } from '@/components/ui'
import { useState, useEffect } from 'react'
import { useToast } from '@/hooks/useToast'
import { useLangStore } from '@/i18n'
import { formatDate } from '@/lib/utils'
import type { LoggingConfig } from '@/types'
import {
    Activity,
    Settings,
    MessageSquare,
    User,
    Volume2,
    Server as ServerIcon,
    ShieldAlert,
    DollarSign,
    Search,
    RefreshCw,
    ChevronLeft,
    ChevronRight,
    FileText,
} from 'lucide-react'

export default function LoggingPage() {
    const { guildId } = useParams<{ guildId: string }>()
    const qc = useQueryClient()
    const toast = useToast()
    const { t, lang } = useLangStore()

    const [activeTab, setActiveTab] = useState<'activities' | 'settings'>('activities')

    // ── Activities Filter & Pagination State ────────────────────────────────
    const [selectedCat, setSelectedCat] = useState<string>('')
    const [search, setSearch] = useState<string>('')
    const [page, setPage] = useState<number>(1)

    // ── Settings Form State ─────────────────────────────────────────────────
    const [form, setForm] = useState<LoggingConfig>({
        messageLogChannelId: null,
        memberLogChannelId: null,
        voiceLogChannelId: null,
        serverLogChannelId: null,
        modLogChannelId: null,
        auditLogChannelId: null,
    })

    const logFields: { key: keyof LoggingConfig; label: string; desc: string }[] = [
        { key: 'messageLogChannelId', label: t.logging.msgLog, desc: t.logging.msgLogDesc },
        { key: 'memberLogChannelId', label: t.logging.memberLog, desc: t.logging.memberLogDesc },
        { key: 'voiceLogChannelId', label: t.logging.voiceLog, desc: t.logging.voiceLogDesc },
        { key: 'serverLogChannelId', label: t.logging.serverLog, desc: t.logging.serverLogDesc },
        {
            key: 'modLogChannelId',
            label: lang === 'vi' ? 'Kênh ghi nhận Quản trị (Mod Log)' : 'Moderation Log Channel',
            desc: lang === 'vi' ? 'Ghi lại các hành động cảnh báo, kick, ban, timeout' : 'Logs warnings, kicks, bans, and timeouts',
        },
        {
            key: 'auditLogChannelId',
            label: lang === 'vi' ? 'Kênh Audit Log tổng' : 'General Audit Log Channel',
            desc: lang === 'vi' ? 'Ghi lại toàn bộ sự kiện quan trọng và thay đổi hệ thống' : 'Logs all major events and system changes',
        },
    ]

    // ── Queries ─────────────────────────────────────────────────────────────
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

    const {
        data: activityData,
        isLoading: loadingActivities,
        refetch: refetchActivities,
        isFetching: fetchingActivities,
    } = useQuery({
        queryKey: ['activityLogs', guildId, selectedCat, search, page],
        queryFn: () =>
            loggingApi.getActivities(guildId!, {
                category: selectedCat !== '' ? Number(selectedCat) : undefined,
                search: search.trim() || undefined,
                page,
                pageSize: 15,
            }),
        enabled: !!guildId && activeTab === 'activities',
        refetchInterval: 10000,
    })

    useEffect(() => {
        if (config) setForm(config)
    }, [config])

    const { mutate: save, isPending } = useMutation({
        mutationFn: () => loggingApi.update(guildId!, form),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ['logging', guildId] })
            toast.success(t.common.savedSuccess)
        },
        onError: () => toast.error(t.common.error),
    })

    const textChannels = guildInfo?.channels.filter((c) => c.type === 'text') ?? []

    const getCategoryMeta = (cat: number) => {
        switch (cat) {
            case 3:
                return { label: lang === 'vi' ? 'Tin nhắn' : 'Message', variant: 'default' as const, icon: MessageSquare }
            case 1:
                return { label: lang === 'vi' ? 'Thành viên' : 'Member', variant: 'warning' as const, icon: User }
            case 2:
                return { label: lang === 'vi' ? 'Thoại / Voice' : 'Voice', variant: 'warning' as const, icon: Volume2 }
            case 7:
                return { label: lang === 'vi' ? 'Máy chủ' : 'Server', variant: 'default' as const, icon: ServerIcon }
            case 4:
                return { label: lang === 'vi' ? 'Quản trị' : 'Moderation', variant: 'danger' as const, icon: ShieldAlert }
            case 5:
                return { label: lang === 'vi' ? 'Bảo mật' : 'Security', variant: 'danger' as const, icon: ShieldAlert }
            case 6:
                return { label: lang === 'vi' ? 'Kinh tế' : 'Economy', variant: 'success' as const, icon: DollarSign }
            case 8:
                return { label: 'Audit', variant: 'default' as const, icon: FileText }
            default:
                return { label: lang === 'vi' ? 'Hệ thống' : 'System', variant: 'default' as const, icon: Activity }
        }
    }

    if (loadingConfig || loadingInfo) return <div className="flex justify-center py-20"><Spinner size="lg" /></div>

    const totalPages = activityData ? Math.ceil(activityData.totalCount / activityData.pageSize) : 1

    return (
        <div className="max-w-5xl space-y-6">
            {/* Header & Tabs */}
            <div>
                <h2 className="text-xl font-bold text-[--color-text] flex items-center gap-2">
                    <Activity className="text-[--color-brand]" />
                    {t.logging.title}
                </h2>
                <p className="text-sm text-[--color-text-muted] mt-1">
                    {lang === 'vi'
                        ? 'Xem nhật ký hoạt động trực tiếp của máy chủ hoặc cấu hình các kênh thông báo tự động cho Bot.'
                        : 'View live server activity logs or configure automated notification channels for the bot.'}
                </p>

                <div className="flex border-b border-[--color-border] gap-6 mt-6">
                    <button
                        onClick={() => setActiveTab('activities')}
                        className={`pb-3 font-medium text-sm flex items-center gap-2 border-b-2 transition-colors cursor-pointer ${
                            activeTab === 'activities'
                                ? 'border-[--color-brand] text-[--color-brand]'
                                : 'border-transparent text-[--color-text-muted] hover:text-[--color-text]'
                        }`}
                    >
                        <Activity size={17} />
                        {lang === 'vi' ? 'Nhật Ký Hoạt Động (Live Audit Logs)' : 'Activity Audit Logs'}
                    </button>
                    <button
                        onClick={() => setActiveTab('settings')}
                        className={`pb-3 font-medium text-sm flex items-center gap-2 border-b-2 transition-colors cursor-pointer ${
                            activeTab === 'settings'
                                ? 'border-[--color-brand] text-[--color-brand]'
                                : 'border-transparent text-[--color-text-muted] hover:text-[--color-text]'
                        }`}
                    >
                        <Settings size={17} />
                        {lang === 'vi' ? 'Cài Đặt Kênh Thông Báo' : 'Log Channel Settings'}
                    </button>
                </div>
            </div>

            {/* TAB 1: ACTIVITY LOGS */}
            {activeTab === 'activities' && (
                <div className="space-y-4">
                    {/* Filters & Controls */}
                    <Card className="bg-[--color-surface] py-4 px-5">
                        <div className="flex flex-col sm:flex-row items-center justify-between gap-3">
                            <div className="flex items-center gap-2 w-full sm:w-auto">
                                <Select
                                    value={selectedCat}
                                    onChange={(e) => {
                                        setSelectedCat(e.target.value)
                                        setPage(1)
                                    }}
                                    className="w-full sm:w-48 text-xs"
                                >
                                    <option value="" className="bg-[--color-surface-alt]">{lang === 'vi' ? 'Tất cả danh mục' : 'All Categories'}</option>
                                    <option value="3" className="bg-[--color-surface-alt]">{lang === 'vi' ? 'Tin nhắn (Message)' : 'Message'}</option>
                                    <option value="1" className="bg-[--color-surface-alt]">{lang === 'vi' ? 'Thành viên (Member)' : 'Member'}</option>
                                    <option value="2" className="bg-[--color-surface-alt]">{lang === 'vi' ? 'Thoại (Voice)' : 'Voice'}</option>
                                    <option value="7" className="bg-[--color-surface-alt]">{lang === 'vi' ? 'Máy chủ (Server)' : 'Server'}</option>
                                    <option value="4" className="bg-[--color-surface-alt]">{lang === 'vi' ? 'Quản trị (Moderation)' : 'Moderation'}</option>
                                    <option value="5" className="bg-[--color-surface-alt]">{lang === 'vi' ? 'Bảo mật (Security)' : 'Security'}</option>
                                    <option value="6" className="bg-[--color-surface-alt]">{lang === 'vi' ? 'Kinh tế (Economy)' : 'Economy'}</option>
                                    <option value="8" className="bg-[--color-surface-alt]">Audit</option>
                                </Select>
                            </div>

                            <div className="flex items-center gap-2 w-full sm:w-auto">
                                <div className="relative flex-1 sm:w-64">
                                    <Search size={14} className="absolute left-3 top-2.5 text-[--color-text-muted]" />
                                    <input
                                        type="text"
                                        placeholder={lang === 'vi' ? 'Tìm nội dung, user...' : 'Search logs, users...'}
                                        value={search}
                                        onChange={(e) => {
                                            setSearch(e.target.value)
                                            setPage(1)
                                        }}
                                        className="w-full pl-8 pr-3 py-1.5 rounded-md bg-[--color-surface-alt] border border-[--color-border] text-xs text-[--color-text] focus:ring-2 focus:ring-[--color-brand] outline-none"
                                    />
                                </div>
                                <Button
                                    variant="secondary"
                                    size="sm"
                                    onClick={() => refetchActivities()}
                                    disabled={fetchingActivities}
                                    title={lang === 'vi' ? 'Làm mới' : 'Refresh'}
                                    className="px-2.5"
                                >
                                    <RefreshCw size={14} className={fetchingActivities ? 'animate-spin' : ''} />
                                </Button>
                            </div>
                        </div>
                    </Card>

                    {/* Logs Feed */}
                    {loadingActivities ? (
                        <div className="flex justify-center py-16"><Spinner size="lg" /></div>
                    ) : !activityData || activityData.logs.length === 0 ? (
                        <Card className="text-center py-16 bg-[--color-surface] text-[--color-text-muted]">
                            <FileText size={36} className="mx-auto mb-2 opacity-40" />
                            <p className="font-medium text-sm">
                                {lang === 'vi' ? 'Chưa có nhật ký hoạt động nào được ghi lại.' : 'No activity logs recorded yet.'}
                            </p>
                            <p className="text-xs mt-1">
                                {lang === 'vi' ? 'Các sự kiện trên máy chủ sẽ hiển thị tại đây theo thời gian thực.' : 'Server events will appear here in real-time.'}
                            </p>
                        </Card>
                    ) : (
                        <div className="space-y-3">
                            {activityData.logs.map((item) => {
                                const meta = getCategoryMeta(item.category)
                                const IconComp = meta.icon
                                return (
                                    <Card key={item.id} className="bg-[--color-surface] p-4 transition-colors hover:border-[--color-border-hover]">
                                        <div className="flex items-start justify-between gap-4">
                                            <div className="flex items-start gap-3 flex-1">
                                                <div className="p-2 rounded-lg bg-[--color-surface-alt] text-[--color-brand] shrink-0 mt-0.5">
                                                    <IconComp size={18} />
                                                </div>
                                                <div className="space-y-1 flex-1">
                                                    <div className="flex items-center gap-2 flex-wrap">
                                                        <Badge variant={meta.variant} className="text-[10px] uppercase font-semibold">
                                                            {meta.label}
                                                        </Badge>
                                                        <h4 className="font-semibold text-sm text-[--color-text] leading-snug">
                                                            {item.title}
                                                        </h4>
                                                    </div>
                                                    {item.description && (
                                                        <p className="text-xs text-[--color-text-muted] whitespace-pre-wrap font-mono bg-[--color-surface-alt] p-2.5 rounded border border-[--color-border]/50 mt-1.5 leading-relaxed">
                                                            {item.description}
                                                        </p>
                                                    )}
                                                    {item.actorUsername && (
                                                        <div className="flex items-center gap-1.5 pt-1 text-xs text-[--color-text-muted]">
                                                            {item.actorAvatarUrl ? (
                                                                <img src={item.actorAvatarUrl} alt="actor" className="w-4 h-4 rounded-full" />
                                                            ) : (
                                                                <User size={12} />
                                                            )}
                                                            <span>
                                                                {lang === 'vi' ? 'Bởi:' : 'By:'} <strong className="text-[--color-text] font-medium">{item.actorUsername}</strong>
                                                            </span>
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                            <span className="text-[11px] text-[--color-text-muted] whitespace-nowrap shrink-0">
                                                {formatDate(item.createdAt)}
                                            </span>
                                        </div>
                                    </Card>
                                )
                            })}

                            {/* Pagination */}
                            {totalPages > 1 && (
                                <div className="flex items-center justify-between pt-2">
                                    <span className="text-xs text-[--color-text-muted]">
                                        {lang === 'vi'
                                            ? `Trang ${page} / ${totalPages} (${activityData.totalCount} bản ghi)`
                                            : `Page ${page} of ${totalPages} (${activityData.totalCount} entries)`}
                                    </span>
                                    <div className="flex items-center gap-2">
                                        <Button
                                            variant="secondary"
                                            size="sm"
                                            onClick={() => setPage((p) => Math.max(1, p - 1))}
                                            disabled={page <= 1}
                                            className="px-2.5"
                                        >
                                            <ChevronLeft size={16} />
                                        </Button>
                                        <Button
                                            variant="secondary"
                                            size="sm"
                                            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                                            disabled={page >= totalPages}
                                            className="px-2.5"
                                        >
                                            <ChevronRight size={16} />
                                        </Button>
                                    </div>
                                </div>
                            )}
                        </div>
                    )}
                </div>
            )}

            {/* TAB 2: CHANNEL SETTINGS */}
            {activeTab === 'settings' && (
                <div className="space-y-6 max-w-2xl">
                    <Card className="space-y-5 bg-[--color-surface]">
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
            )}
        </div>
    )
}