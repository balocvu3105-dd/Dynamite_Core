import { avatarUrl } from '@/lib/utils'
import type { DiscordUser } from '@/types'

interface UserProfileHeaderProps {
    user: DiscordUser
}

export function UserProfileHeader({ user }: UserProfileHeaderProps) {
    const handle = user.username.startsWith('@') ? user.username : `@${user.username.toLowerCase().replace(/\s+/g, '')}`

    return (
        <div className="flex items-center gap-5 py-6">
            <div className="relative">
                <img
                    src={avatarUrl(user)}
                    alt={user.username}
                    className="w-20 h-20 sm:w-24 sm:h-24 rounded-full border-2 border-[#ff2a85]/40 object-cover shadow-[0_0_20px_rgba(255,42,133,0.25)] bg-[--color-surface-raised]"
                />
                <span className="absolute bottom-1 right-1 w-4 h-4 rounded-full bg-emerald-500 border-2 border-[--color-surface] shadow-sm animate-pulse" title="Online" />
            </div>
            <div className="flex flex-col justify-center">
                <h1 className="text-2xl sm:text-3xl font-extrabold text-[--color-text] tracking-tight">
                    {user.username}
                </h1>
                <p className="text-base sm:text-lg font-medium text-[#ff2a85] mt-0.5 tracking-wide">
                    {handle}
                </p>
            </div>
        </div>
    )
}
