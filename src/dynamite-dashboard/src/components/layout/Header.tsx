import { LogOut, Globe } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { useLangStore } from '@/i18n'
import { avatarUrl } from '@/lib/utils'
import { Button } from '@/components/ui'

interface HeaderProps {
    title: string
}

export function Header({ title }: HeaderProps) {
    const navigate = useNavigate()
    const { user, logout } = useAuthStore()
    const { lang, toggleLanguage, t } = useLangStore()

    const handleLogout = () => {
        logout()
        navigate('/login', { replace: true })
    }

    return (
        <header className="h-14 border-b border-[--color-border] bg-[--color-surface-alt] flex items-center justify-between px-6">
            <div className="flex items-center gap-4">
                <h1 className="text-base font-semibold text-[--color-text]">{title}</h1>
                <div className="hidden sm:flex items-center gap-2 px-2.5 py-1 rounded-full bg-[--color-surface] border border-[--color-border] text-xs font-medium text-emerald-400">
                    <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                    <span>{lang === 'vi' ? 'Bot Đang Hoạt Động (24ms)' : 'Bot Online (24ms)'}</span>
                </div>
            </div>

            <div className="flex items-center gap-3">
                <Button
                    variant="secondary"
                    size="sm"
                    onClick={toggleLanguage}
                    className="gap-2 px-3 py-1.5 font-medium border border-[--color-border] bg-[--color-surface] hover:bg-[--color-surface-hover] transition-colors cursor-pointer"
                >
                    <Globe size={15} className="text-[--color-brand]" />
                    <span>{lang === 'vi' ? '🇻🇳 Tiếng Việt' : '🇬🇧 English'}</span>
                </Button>

                {user && (
                    <div
                        onClick={() => navigate('/servers')}
                        className="flex items-center gap-2 cursor-pointer hover:bg-[--color-surface] px-2 py-1 rounded-full transition-all border border-transparent hover:border-[#ff2a85]/40"
                        title={lang === 'vi' ? 'Xem hồ sơ cá nhân & máy chủ' : 'View personal profile & servers'}
                    >
                        <img
                            src={avatarUrl(user)}
                            alt={user.username}
                            className="w-7 h-7 rounded-full border border-[#ff2a85]/40 object-cover"
                        />
                        <span className="text-sm font-bold text-[--color-text] hover:text-[#ff2a85] transition-colors">{user.username}</span>
                    </div>
                )}
                <Button variant="ghost" size="sm" onClick={handleLogout} className="gap-2 cursor-pointer">
                    <LogOut size={14} />
                    {t.common.logout}
                </Button>
            </div>
        </header>
    )
}