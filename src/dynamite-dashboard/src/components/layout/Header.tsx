import { LogOut } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { avatarUrl } from '@/lib/utils'
import { Button } from '@/components/ui'

interface HeaderProps {
    title: string
}

export function Header({ title }: HeaderProps) {
    const navigate = useNavigate()
    const { user, logout } = useAuthStore()

    const handleLogout = () => {
        logout()
        navigate('/login', { replace: true })
    }

    return (
        <header className="h-14 border-b border-[--color-border] bg-[--color-surface-alt] flex items-center justify-between px-6">
            <h1 className="text-base font-semibold text-[--color-text]">{title}</h1>

            <div className="flex items-center gap-3">
                {user && (
                    <div className="flex items-center gap-2">
                        <img
                            src={avatarUrl(user)}
                            alt={user.username}
                            className="w-7 h-7 rounded-full"
                        />
                        <span className="text-sm text-[--color-text-muted]">{user.username}</span>
                    </div>
                )}
                <Button variant="ghost" size="sm" onClick={handleLogout} className="gap-2">
                    <LogOut size={14} />
                    Logout
                </Button>
            </div>
        </header>
    )
}