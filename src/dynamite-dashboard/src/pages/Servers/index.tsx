import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { guildsApi } from '@/api'
import { useAuthStore } from '@/store/authStore'
import { useGuildStore } from '@/store/guildStore'
import { useLangStore } from '@/i18n'
import { Spinner, Card } from '@/components/ui'
import type { GuildSummary } from '@/types'

import { UserProfileHeader } from './components/UserProfileHeader'
import { UserNavTabs, type NavTabType } from './components/UserNavTabs'
import { ServersView } from './components/ServersView'
import { MembershipView } from './components/MembershipView'
import { UserSettingsView } from './components/UserSettingsView'

export default function ServersPage() {
    const navigate = useNavigate()
    const { user, logout } = useAuthStore()
    const setSelectedGuild = useGuildStore((s) => s.setSelectedGuild)
    const { lang, toggleLanguage } = useLangStore()

    const [activeTab, setActiveTab] = useState<NavTabType>('servers')

    const { data: guilds, isLoading, isError } = useQuery({
        queryKey: ['guilds'],
        queryFn: guildsApi.list,
        refetchInterval: 3000,
    })

    const handleSelectGuild = (guild: GuildSummary) => {
        if (!guild.botPresent) return
        setSelectedGuild(guild)
        navigate(`/dashboard/${guild.id}/overview`)
    }

    const handleLogout = () => {
        logout()
        navigate('/login', { replace: true })
    }

    if (!user) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-[--color-surface]">
                <Spinner size="lg" />
            </div>
        )
    }

    return (
        <div className="min-h-screen bg-[--color-surface] pb-16">
            {/* Top Minimal Brand Bar */}
            <header className="border-b border-[--color-border] bg-[--color-surface-alt]">
                <div className="max-w-5xl mx-auto px-6 h-14 flex items-center justify-between">
                    <div
                        onClick={() => navigate('/')}
                        className="flex items-center gap-2 cursor-pointer hover:opacity-80 transition-opacity"
                    >
                        <div className="w-7 h-7 rounded-md bg-gradient-to-tr from-[#ff2a85] to-amber-500 flex items-center justify-center shadow-sm">
                            <span className="text-white text-xs font-bold">D</span>
                        </div>
                        <span className="font-extrabold text-[--color-text] tracking-tight">Dynamite Core</span>
                    </div>
                </div>
            </header>

            {/* OOP Encapsulated Profile Layout Container */}
            <main className="max-w-5xl mx-auto px-6 pt-4">
                {/* 1. User Profile Header Component */}
                <UserProfileHeader user={user} />

                {/* 2. Primary Navigation Tabs Component */}
                <UserNavTabs activeTab={activeTab} onTabChange={setActiveTab} lang={lang} />

                {/* 3. Tab Content Area */}
                <div className="mt-6">
                    {isLoading && (
                        <div className="flex items-center justify-center py-24">
                            <Spinner size="lg" />
                        </div>
                    )}

                    {isError && (
                        <Card className="text-center py-12">
                            <p className="text-[--color-danger] font-semibold text-lg">
                                {lang === 'vi' ? 'Không thể tải danh sách máy chủ' : 'Failed to load servers'}
                            </p>
                            <p className="text-[--color-text-muted] text-sm mt-1">
                                {lang === 'vi' ? 'Vui lòng kiểm tra API hoặc thử lại sau.' : 'Please check API connection and try again.'}
                            </p>
                        </Card>
                    )}

                    {!isLoading && !isError && guilds && activeTab === 'servers' && (
                        <ServersView guilds={guilds} onSelectGuild={handleSelectGuild} lang={lang} />
                    )}

                    {activeTab === 'membership' && (
                        <MembershipView user={user} lang={lang} />
                    )}

                    {activeTab === 'settings' && (
                        <UserSettingsView user={user} lang={lang} onToggleLang={toggleLanguage} onLogout={handleLogout} />
                    )}
                </div>
            </main>
        </div>
    )
}