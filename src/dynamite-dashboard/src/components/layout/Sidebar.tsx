import { NavLink, useParams, useNavigate } from 'react-router-dom'
import {
    LayoutDashboard,
    Shield,
    FileText,
    MessageSquare,
    Lock,
    ChevronLeft,
    Wand2,
    Coins,
    Terminal,
    Ban,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { useGuildStore } from '@/store/guildStore'
import { useLangStore } from '@/i18n'

export function Sidebar() {
    const { guildId } = useParams<{ guildId: string }>()
    const navigate = useNavigate()
    const selectedGuild = useGuildStore((s) => s.selectedGuild)
    const { lang } = useLangStore()

    const navItems = [
        { label: lang === 'vi' ? 'Tổng quan' : 'Overview', icon: LayoutDashboard, path: 'overview' },
        { label: lang === 'vi' ? 'Danh sách Lệnh' : 'Commands List', icon: Terminal, path: 'commands' },
        { label: lang === 'vi' ? 'Danh sách Cấm/Bỏ qua' : 'Blacklist & Ignore', icon: Ban, path: 'blacklist' },
        { label: lang === 'vi' ? 'Chuyên gia Thiết lập' : 'Server Setup', icon: Wand2, path: 'setup' },
        { label: lang === 'vi' ? 'Kinh tế & Câu cá' : 'Economy & Fishing', icon: Coins, path: 'economy' },
        { label: lang === 'vi' ? 'Quản trị viên' : 'Moderation', icon: Shield, path: 'moderation' },
        { label: lang === 'vi' ? 'Nhật ký Hoạt động' : 'Logging', icon: FileText, path: 'logging' },
        { label: lang === 'vi' ? 'Lời chào mừng' : 'Welcome', icon: MessageSquare, path: 'welcome' },
        { label: lang === 'vi' ? 'Bảo mật Máy chủ' : 'Security', icon: Lock, path: 'security' },
    ]

    return (
        <aside className="w-60 min-h-screen bg-[--color-surface-alt] border-r border-[--color-border] flex flex-col">
            {/* Guild header */}
            <div className="p-4 border-b border-[--color-border]">
                <button
                    onClick={() => navigate('/servers')}
                    className="flex items-center gap-2 text-[--color-text-muted] hover:text-[--color-text] text-xs mb-3 transition-colors cursor-pointer"
                >
                    <ChevronLeft size={14} />
                    {lang === 'vi' ? 'Tất cả máy chủ' : 'All servers'}
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
            <nav className="flex-1 p-2 flex flex-col gap-1.5 overflow-y-auto">
                {navItems.map(({ label, icon: Icon, path }) => (
                    <NavLink
                        key={path}
                        to={`/dashboard/${guildId}/${path}`}
                        className={({ isActive }) =>
                            cn(
                                'flex items-center gap-3 px-3.5 py-2.5 rounded-lg text-sm transition-all duration-200 cursor-pointer',
                                isActive
                                    ? 'bg-[--color-brand] text-white font-bold ring-2 ring-white ring-offset-2 ring-offset-[--color-surface-alt] shadow-md shadow-[--color-brand]/40 scale-[1.02]'
                                    : 'text-[--color-text-muted] hover:text-[--color-text] hover:bg-[--color-surface-raised]'
                            )
                        }
                    >
                        <Icon size={17} />
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