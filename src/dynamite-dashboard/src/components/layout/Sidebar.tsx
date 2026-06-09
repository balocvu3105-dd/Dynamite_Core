import { NavLink, useParams, useNavigate } from 'react-router-dom'
import {
    LayoutDashboard,
    Shield,
    FileText,
    MessageSquare,
    Lock,
    ChevronLeft,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { useGuildStore } from '@/store/guildStore'

const NAV_ITEMS = [
    { label: 'Overview', icon: LayoutDashboard, path: 'overview' },
    { label: 'Moderation', icon: Shield, path: 'moderation' },
    { label: 'Logging', icon: FileText, path: 'logging' },
    { label: 'Welcome', icon: MessageSquare, path: 'welcome' },
    { label: 'Security', icon: Lock, path: 'security' },
]

export function Sidebar() {
    const { guildId } = useParams<{ guildId: string }>()
    const navigate = useNavigate()
    const selectedGuild = useGuildStore((s) => s.selectedGuild)

    return (
        <aside className="w-60 min-h-screen bg-[--color-surface-alt] border-r border-[--color-border] flex flex-col">
            {/* Guild header */}
            <div className="p-4 border-b border-[--color-border]">
                <button
                    onClick={() => navigate('/servers')}
                    className="flex items-center gap-2 text-[--color-text-muted] hover:text-[--color-text] text-xs mb-3 transition-colors"
                >
                    <ChevronLeft size={14} />
                    All servers
                </button>

                <div className="flex items-center gap-3">
                    {selectedGuild?.iconUrl ? (
                        <img
                            src={selectedGuild.iconUrl}
                            alt={selectedGuild.name}
                            className="w-9 h-9 rounded-full"
                        />
                    ) : (
                        <div className="w-9 h-9 rounded-full bg-[--color-brand] flex items-center justify-center text-white text-sm font-bold">
                            {selectedGuild?.name?.[0] ?? '?'}
                        </div>
                    )}
                    <div className="overflow-hidden">
                        <p className="text-sm font-semibold text-[--color-text] truncate">
                            {selectedGuild?.name ?? 'Server'}
                        </p>
                        <p className="text-xs text-[--color-text-muted]">Dashboard</p>
                    </div>
                </div>
            </div>

            {/* Nav items */}
            <nav className="flex-1 p-2 flex flex-col gap-0.5">
                {NAV_ITEMS.map(({ label, icon: Icon, path }) => (
                    <NavLink
                        key={path}
                        to={`/dashboard/${guildId}/${path}`}
                        className={({ isActive }) =>
                            cn(
                                'flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors',
                                isActive
                                    ? 'bg-[--color-brand]/15 text-[--color-brand] font-medium'
                                    : 'text-[--color-text-muted] hover:text-[--color-text] hover:bg-[--color-surface-raised]'
                            )
                        }
                    >
                        <Icon size={16} />
                        {label}
                    </NavLink>
                ))}
            </nav>

            {/* Version */}
            <div className="p-4 border-t border-[--color-border]">
                <p className="text-xs text-[--color-text-muted]">Dynamite v1.0</p>
            </div>
        </aside>
    )
}