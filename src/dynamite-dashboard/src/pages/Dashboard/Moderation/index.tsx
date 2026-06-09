import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { moderationApi } from '@/api'
import { Card, Badge, Button, Spinner } from '@/components/ui'
import { formatDate } from '@/lib/utils'
import { Trash2 } from 'lucide-react'
import { useState } from 'react'
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
    const [tab, setTab] = useState<Tab>('warnings')
    const [page, setPage] = useState(1)

    const { data: warnings, isLoading: loadingWarnings } = useQuery({
        queryKey: ['warnings', guildId, page],
        queryFn: () => moderationApi.getWarnings(guildId!, page),
        enabled: !!guildId && tab === 'warnings',
    })

    const { data: modlogs, isLoading: loadingLogs } = useQuery({
        queryKey: ['modlogs', guildId, page],
        queryFn: () => moderationApi.getModLogs(guildId!, page),
        enabled: !!guildId && tab === 'modlogs',
    })

    const { mutate: deleteWarning, isPending: deleting } = useMutation({
        mutationFn: (warningId: string) => moderationApi.deleteWarning(guildId!, warningId),
        onSuccess: () => qc.invalidateQueries({ queryKey: ['warnings', guildId] }),
    })

    const isLoading = tab === 'warnings' ? loadingWarnings : loadingLogs
    const total = tab === 'warnings' ? (warnings?.total ?? 0) : (modlogs?.total ?? 0)
    const totalPages = Math.ceil(total / 20)

    return (
        <div className="max-w-4xl space-y-6">
            <div>
                <h2 className="text-lg font-semibold text-[--color-text]">Moderation</h2>
                <p className="text-sm text-[--color-text-muted] mt-1">
                    View warnings and moderation history.
                </p>
            </div>

            {/* Tabs */}
            <div className="flex gap-1 border-b border-[--color-border]">
                {(['warnings', 'modlogs'] as Tab[]).map((t) => (
                    <button
                        key={t}
                        onClick={() => { setTab(t); setPage(1) }}
                        className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors capitalize ${tab === t
                                ? 'border-[--color-brand] text-[--color-brand]'
                                : 'border-transparent text-[--color-text-muted] hover:text-[--color-text]'
                            }`}
                    >
                        {t === 'warnings' ? 'Warnings' : 'Mod Logs'}
                        {t === 'warnings' && warnings?.total
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
                        <p className="text-center text-[--color-text-muted] py-10 text-sm">No warnings found.</p>
                    ) : (
                        <table className="w-full text-sm">
                            <thead>
                                <tr className="border-b border-[--color-border] text-[--color-text-muted] text-xs uppercase">
                                    <th className="px-4 py-3 text-left">User ID</th>
                                    <th className="px-4 py-3 text-left">Reason</th>
                                    <th className="px-4 py-3 text-left">Date</th>
                                    <th className="px-4 py-3 text-right">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {warnings?.items.map((w: Warning) => (
                                    <tr key={w.id} className="border-b border-[--color-border] last:border-0 hover:bg-[--color-surface-raised]">
                                        <td className="px-4 py-3 font-mono text-xs text-[--color-text-muted]">{w.userId}</td>
                                        <td className="px-4 py-3 text-[--color-text]">{w.reason}</td>
                                        <td className="px-4 py-3 text-[--color-text-muted] text-xs whitespace-nowrap">{formatDate(w.createdAt)}</td>
                                        <td className="px-4 py-3 text-right">
                                            <Button
                                                variant="ghost"
                                                size="sm"
                                                disabled={deleting}
                                                onClick={() => deleteWarning(w.id)}
                                                className="text-[--color-danger] hover:text-[--color-danger]"
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
                        <p className="text-center text-[--color-text-muted] py-10 text-sm">No mod logs found.</p>
                    ) : (
                        <table className="w-full text-sm">
                            <thead>
                                <tr className="border-b border-[--color-border] text-[--color-text-muted] text-xs uppercase">
                                    <th className="px-4 py-3 text-left">Action</th>
                                    <th className="px-4 py-3 text-left">Target</th>
                                    <th className="px-4 py-3 text-left">Reason</th>
                                    <th className="px-4 py-3 text-left">Date</th>
                                </tr>
                            </thead>
                            <tbody>
                                {modlogs?.items.map((log: ModLog) => (
                                    <tr key={log.id} className="border-b border-[--color-border] last:border-0 hover:bg-[--color-surface-raised]">
                                        <td className="px-4 py-3">
                                            <Badge variant={ACTION_VARIANT[log.action] ?? 'default'}>
                                                {log.action}
                                            </Badge>
                                        </td>
                                        <td className="px-4 py-3 font-mono text-xs text-[--color-text-muted]">{log.targetUserId}</td>
                                        <td className="px-4 py-3 text-[--color-text]">{log.reason ?? '—'}</td>
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
                        Page {page} of {totalPages} — {total} total
                    </span>
                    <div className="flex gap-2">
                        <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                            Previous
                        </Button>
                        <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
                            Next
                        </Button>
                    </div>
                </div>
            )}
        </div>
    )
}