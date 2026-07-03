import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { moderationApi } from '@/api'
import { Card, Badge, Button, Spinner } from '@/components/ui'
import { formatDate } from '@/lib/utils'
import { Trash2, ShieldAlert, ScrollText } from 'lucide-react'
import { useState } from 'react'
import { useToast } from '@/hooks/useToast'
import { useConfirm } from '@/hooks/useConfirm'
import { useLangStore } from '@/i18n'
import type { Warning, ModLog } from '@/types'

type Tab = 'warnings' | 'modlogs'

const ACTION_VARIANT: Record<string, 'danger' | 'warning' | 'default'> = {
    ban: 'danger',
    kick: 'warning',
    timeout: 'warning',
    warn: 'warning',
    unban: 'default',
    untimeout: 'default',
}

export default function ModerationPage() {
    const { guildId } = useParams<{ guildId: string }>()
    const qc = useQueryClient()
    const toast = useToast()
    const confirm = useConfirm()
    const { t, lang } = useLangStore()
    const [tab, setTab] = useState<Tab>('warnings')
    const [page, setPage] = useState(1)

    const { data: warnings, isLoading: loadingWarnings } = useQuery({
        queryKey: ['warnings', guildId, page],
        queryFn: () => moderationApi.getWarnings(guildId!, page),
        enabled: !!guildId && tab === 'warnings',
        refetchInterval: 1500,
    })

    const { data: modlogs, isLoading: loadingLogs } = useQuery({
        queryKey: ['modlogs', guildId, page],
        queryFn: () => moderationApi.getModLogs(guildId!, page),
        enabled: !!guildId && tab === 'modlogs',
        refetchInterval: 1500,
    })

    const { mutate: deleteWarning, isPending: deleting } = useMutation({
        mutationFn: (warningId: string) => moderationApi.deleteWarning(guildId!, warningId),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ['warnings', guildId] })
            toast.success(lang === 'vi' ? 'Đã xóa cảnh báo.' : 'Warning deleted.')
        },
        onError: () => toast.error(lang === 'vi' ? 'Xóa cảnh báo thất bại.' : 'Failed to delete warning.'),
    })

    const handleDeleteWarning = async (warningId: string) => {
        const ok = await confirm({
            title: lang === 'vi' ? 'Xóa cảnh báo vi phạm' : 'Delete warning',
            message: lang === 'vi' ? 'Hành động này không thể hoàn tác. Bạn có chắc chắn muốn gỡ cảnh báo này?' : 'This action cannot be undone. Are you sure you want to remove this warning?',
            confirmLabel: lang === 'vi' ? 'Xóa ngay' : 'Delete',
            cancelLabel: t.common.cancel,
            variant: 'danger',
        })
        if (ok) deleteWarning(warningId)
    }

    const isLoading = tab === 'warnings' ? loadingWarnings : loadingLogs
    const total = tab === 'warnings' ? (warnings?.total ?? 0) : (modlogs?.total ?? 0)
    const totalPages = Math.ceil(total / 20)

    return (
        <div className="max-w-4xl space-y-6">
            <div>
                <h2 className="text-lg font-semibold text-[--color-text]">{t.moderation.title}</h2>
                <p className="text-sm text-[--color-text-muted] mt-1">
                    {t.moderation.subtitle}
                </p>
            </div>

            {/* Tabs */}
            <div className="flex gap-1 border-b border-[--color-border]">
                {(['warnings', 'modlogs'] as Tab[]).map((tItem) => (
                    <button
                        key={tItem}
                        onClick={() => { setTab(tItem); setPage(1) }}
                        className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors cursor-pointer ${tab === tItem
                            ? 'border-[--color-brand] text-[--color-brand]'
                            : 'border-transparent text-[--color-text-muted] hover:text-[--color-text]'
                            }`}
                    >
                        {tItem === 'warnings' ? t.moderation.recentWarnings : t.moderation.auditLogs}
                        {tItem === 'warnings' && warnings?.total
                            ? <span className="ml-1.5 text-xs bg-[--color-surface-raised] px-1.5 py-0.5 rounded-full">{warnings.total}</span>
                            : null}
                    </button>
                ))}
            </div>

            {isLoading && <div className="flex justify-center py-20"><Spinner size="lg" /></div>}

            {/* Warnings table */}
            {!isLoading && tab === 'warnings' && (
                <Card className="p-0 overflow-hidden">
                    {warnings?.items.length === 0 ? (
                        <EmptyState
                            icon={<ShieldAlert size={32} className="text-[--color-text-muted]" />}
                            title={t.moderation.noWarnings}
                            description={lang === 'vi' ? 'Máy chủ này chưa ghi nhận cảnh báo nào.' : 'This server has no recorded warnings.'}
                        />
                    ) : (
                        <table className="w-full text-sm">
                            <thead>
                                <tr className="border-b border-[--color-border] text-[--color-text-muted] text-xs uppercase">
                                    <th className="px-4 py-3 text-left">{lang === 'vi' ? 'Thành Viên Vi Phạm (Tên & ID)' : 'Offender (Name & ID)'}</th>
                                    <th className="px-4 py-3 text-left">{t.moderation.reason}</th>
                                    <th className="px-4 py-3 text-left">{t.moderation.date}</th>
                                    <th className="px-4 py-3 text-right">{t.common.actions}</th>
                                </tr>
                            </thead>
                            <tbody>
                                {warnings?.items.map((w: Warning) => (
                                    <tr key={w.id} className="border-b border-[--color-border] last:border-0 hover:bg-[--color-surface-raised] transition-colors">
                                        <td className="px-4 py-3">
                                            <div className="flex flex-col">
                                                <span className="font-bold text-[--color-text]">
                                                    {w.targetUsername || `Discord User #${w.userId.slice(-4)}`}
                                                </span>
                                                <span className="font-mono text-xs text-pink-400">{w.userId}</span>
                                            </div>
                                        </td>
                                        <td className="px-4 py-3 text-[--color-text] font-medium">{w.reason}</td>
                                        <td className="px-4 py-3 text-[--color-text-muted] text-xs whitespace-nowrap">{formatDate(w.createdAt)}</td>
                                        <td className="px-4 py-3 text-right">
                                            <Button
                                                variant="ghost"
                                                size="sm"
                                                disabled={deleting}
                                                onClick={() => handleDeleteWarning(w.id)}
                                                className="text-[--color-danger] hover:text-[--color-danger] cursor-pointer"
                                            >
                                                <Trash2 size={14} />
                                            </Button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                </Card>
            )}

            {/* Mod logs table */}
            {!isLoading && tab === 'modlogs' && (
                <Card className="p-0 overflow-hidden">
                    {modlogs?.items.length === 0 ? (
                        <EmptyState
                            icon={<ScrollText size={32} className="text-[--color-text-muted]" />}
                            title={t.moderation.noLogs}
                            description={lang === 'vi' ? 'Chưa có nhật ký xử lý vi phạm nào.' : 'No moderation actions have been recorded yet.'}
                        />
                    ) : (
                        <table className="w-full text-sm">
                            <thead>
                                <tr className="border-b border-[--color-border] text-[--color-text-muted] text-xs uppercase">
                                    <th className="px-4 py-3 text-left">{t.common.actions}</th>
                                    <th className="px-4 py-3 text-left">{lang === 'vi' ? 'Mục Tiêu Vi Phạm (Tên & ID)' : 'Target (Name & ID)'}</th>
                                    <th className="px-4 py-3 text-left">{t.moderation.reason}</th>
                                    <th className="px-4 py-3 text-left">{t.moderation.date}</th>
                                </tr>
                            </thead>
                            <tbody>
                                {modlogs?.items.map((log: ModLog) => (
                                    <tr key={log.id} className="border-b border-[--color-border] last:border-0 hover:bg-[--color-surface-raised] transition-colors">
                                        <td className="px-4 py-3">
                                            <Badge variant={ACTION_VARIANT[log.action] ?? 'default'}>
                                                {log.action}
                                            </Badge>
                                        </td>
                                        <td className="px-4 py-3">
                                            <div className="flex flex-col">
                                                <span className="font-bold text-[--color-text]">
                                                    {log.targetUsername || `Discord User #${log.targetUserId.slice(-4)}`}
                                                </span>
                                                <span className="font-mono text-xs text-pink-400">{log.targetUserId}</span>
                                            </div>
                                        </td>
                                        <td className="px-4 py-3 text-[--color-text] font-medium">{log.reason ?? '—'}</td>
                                        <td className="px-4 py-3 text-[--color-text-muted] text-xs whitespace-nowrap">{formatDate(log.createdAt)}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                </Card>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
                <div className="flex items-center justify-between text-sm">
                    <span className="text-[--color-text-muted]">
                        {lang === 'vi' ? `Trang ${page} / ${totalPages} — Tổng ${total}` : `Page ${page} of ${totalPages} — ${total} total`}
                    </span>
                    <div className="flex gap-2">
                        <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                            {lang === 'vi' ? 'Trang trước' : 'Previous'}
                        </Button>
                        <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
                            {lang === 'vi' ? 'Trang tiếp' : 'Next'}
                        </Button>
                    </div>
                </div>
            )}
        </div>
    )
}

// ─── Shared Empty State ───────────────────────────────────────────────────────

function EmptyState({ icon, title, description }: {
    icon: React.ReactNode
    title: string
    description: string
}) {
    return (
        <div className="flex flex-col items-center justify-center py-16 gap-3 text-center px-6">
            {icon}
            <p className="font-medium text-[--color-text]">{title}</p>
            <p className="text-sm text-[--color-text-muted] max-w-xs">{description}</p>
        </div>
    )
}