import { useState } from 'react'
import { List, Crown, Search, Bot, ExternalLink, Sparkles } from 'lucide-react'
import { Card, Badge } from '@/components/ui'
import { guildIconUrl } from '@/lib/utils'
import type { GuildSummary } from '@/types'

interface ServersViewProps {
    guilds: GuildSummary[]
    onSelectGuild: (guild: GuildSummary) => void
    lang: string
}

type ServerSubTab = 'list' | 'premium'

export function ServersView({ guilds, onSelectGuild, lang }: ServersViewProps) {
    const [subTab, setSubTab] = useState<ServerSubTab>('list')
    const [searchQuery, setSearchQuery] = useState('')

    const filteredGuilds = guilds.filter((g) =>
        g.name.toLowerCase().includes(searchQuery.toLowerCase())
    )

    const activeGuilds = filteredGuilds.filter((g) => g.botPresent)
    const inactiveGuilds = filteredGuilds.filter((g) => !g.botPresent)

    return (
        <div className="space-y-6 animate-fadeIn">
            {/* Sub navigation bar */}
            <div className="grid grid-cols-2 gap-2.5 bg-[--color-surface-alt]/40 p-1.5 rounded-xl border border-[--color-border]">
                <button
                    onClick={() => setSubTab('list')}
                    className={`flex items-center justify-center gap-2 py-3 px-4 rounded-lg font-medium text-sm transition-all cursor-pointer ${
                        subTab === 'list'
                            ? 'bg-[--color-surface-raised] border-b-2 border-[#ff2a85] text-[--color-text] shadow-md'
                            : 'bg-[--color-surface]/40 hover:bg-[--color-surface-hover] text-[--color-text-muted]'
                    }`}
                >
                    <List size={16} className="text-indigo-400" />
                    <span>{lang === 'vi' ? 'Danh Sách' : 'List'}</span>
                </button>
                <button
                    onClick={() => setSubTab('premium')}
                    className={`flex items-center justify-center gap-2 py-3 px-4 rounded-lg font-medium text-sm transition-all cursor-pointer ${
                        subTab === 'premium'
                            ? 'bg-[--color-surface-raised] border-b-2 border-amber-500 text-amber-500 font-bold shadow-md'
                            : 'bg-[--color-surface]/40 hover:bg-[--color-surface-hover] text-[--color-text-muted] hover:text-amber-400'
                    }`}
                >
                    <Crown size={16} className="text-amber-500" />
                    <span>{lang === 'vi' ? 'Nâng Cấp Premium' : 'Premium'}</span>
                </button>
            </div>

            {subTab === 'list' ? (
                <>
                    {/* Header title & counter */}
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pt-2">
                        <div>
                            <h2 className="text-2xl sm:text-3xl font-extrabold text-[--color-text]">
                                {lang === 'vi' ? 'Máy Chủ' : 'Servers'}
                            </h2>
                            <p className="text-sm sm:text-base font-medium text-[--color-text-muted] mt-1">
                                {lang === 'vi'
                                    ? `Các máy chủ bạn đang tham gia (${guilds.length} máy chủ)`
                                    : `Servers you're in (${guilds.length} servers)`}
                            </p>
                        </div>
                        <div className="relative w-full sm:w-64">
                            <Search size={16} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-[--color-text-muted]" />
                            <input
                                type="text"
                                placeholder={lang === 'vi' ? 'Tìm tên máy chủ...' : 'Search servers...'}
                                value={searchQuery}
                                onChange={(e) => setSearchQuery(e.target.value)}
                                className="w-full pl-9 pr-4 py-2 text-sm rounded-lg border border-[--color-border] bg-[--color-surface-raised] text-[--color-text] placeholder-[--color-text-muted] focus:outline-none focus:border-[#ff2a85] transition-colors"
                            />
                        </div>
                    </div>

                    {/* Active servers */}
                    {activeGuilds.length > 0 && (
                        <div className="space-y-3 pt-2">
                            <h3 className="text-xs font-bold text-[#ff2a85] uppercase tracking-wider flex items-center gap-2">
                                <Bot size={14} />
                                {lang === 'vi' ? 'Bot đã cài đặt - Đang hoạt động' : 'Bot Installed - Active'}
                            </h3>
                            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3.5">
                                {activeGuilds.map((guild) => (
                                    <GuildCard key={guild.id} guild={guild} onClick={() => onSelectGuild(guild)} lang={lang} />
                                ))}
                            </div>
                        </div>
                    )}

                    {/* Inactive servers */}
                    {inactiveGuilds.length > 0 && (
                        <div className="space-y-3 pt-4">
                            <h3 className="text-xs font-bold text-[--color-text-muted] uppercase tracking-wider flex items-center gap-2">
                                <ExternalLink size={14} />
                                {lang === 'vi' ? 'Chưa cài đặt Bot' : 'Bot Not Installed'}
                            </h3>
                            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3.5">
                                {inactiveGuilds.map((guild) => (
                                    <GuildCard key={guild.id} guild={guild} onClick={() => onSelectGuild(guild)} lang={lang} />
                                ))}
                            </div>
                        </div>
                    )}

                    {filteredGuilds.length === 0 && (
                        <Card className="text-center py-12 border-dashed">
                            <p className="text-[--color-text-muted] font-medium">
                                {lang === 'vi' ? 'Không tìm thấy máy chủ phù hợp.' : 'No matching servers found.'}
                            </p>
                        </Card>
                    )}
                </>
            ) : (
                /* Premium Perks Subtab View */
                <Card className="p-8 text-center bg-gradient-to-br from-[#ff2a85]/10 via-[--color-surface-raised] to-amber-500/10 border-amber-500/30">
                    <div className="w-16 h-16 rounded-2xl bg-amber-500/20 text-amber-500 flex items-center justify-center mx-auto mb-4 shadow-lg">
                        <Crown size={32} />
                    </div>
                    <h3 className="text-2xl font-bold text-[--color-text]">Dynamite Core Premium Server</h3>
                    <p className="text-sm text-[--color-text-muted] max-w-lg mx-auto mt-2 leading-relaxed">
                        {lang === 'vi'
                            ? 'Mở khóa toàn bộ giới hạn chống spam AI cao cấp, ghi nhận nhật ký vi phạm không giới hạn, và tăng tốc độ xử lý moderation tức thì cho máy chủ của bạn.'
                            : 'Unlock high-tier AI anti-spam protection, unlimited moderation audit retention, and lightning-fast action processing for your Discord server.'}
                    </p>
                    <div className="mt-6 flex justify-center gap-4">
                        <button className="px-6 py-2.5 rounded-lg font-bold text-white bg-gradient-to-r from-amber-500 to-[#ff2a85] hover:opacity-90 transition-opacity shadow-lg flex items-center gap-2">
                            <Sparkles size={16} />
                            {lang === 'vi' ? 'Nâng Cấp Máy Chủ Ngay' : 'Upgrade Server Now'}
                        </button>
                    </div>
                </Card>
            )}
        </div>
    )
}

function GuildCard({ guild, onClick, lang }: { guild: GuildSummary; onClick: () => void; lang: string }) {
    const botInviteUrl = `https://discord.com/api/oauth2/authorize?client_id=${import.meta.env.VITE_DISCORD_CLIENT_ID}&permissions=8&scope=bot%20applications.commands&guild_id=${guild.id}`

    return (
        <Card
            className={`flex items-center gap-3.5 p-4 transition-all ${
                guild.botPresent
                    ? 'hover:border-[#ff2a85]/60 hover:bg-[--color-surface-raised] cursor-pointer shadow-sm hover:shadow-[0_4px_15px_rgba(255,42,133,0.15)]'
                    : 'opacity-70 hover:opacity-100 bg-[--color-surface]/50'
            }`}
            onClick={guild.botPresent ? onClick : undefined}
        >
            <img
                src={guildIconUrl(guild)}
                alt={guild.name}
                className="w-12 h-12 rounded-full flex-shrink-0 border border-[--color-border] object-cover"
            />
            <div className="flex-1 min-w-0">
                <p className="text-sm font-bold text-[--color-text] truncate">{guild.name}</p>
                <div className="mt-1">
                    {guild.botPresent ? (
                        <Badge variant="success" className="text-[11px] font-semibold">
                            <Bot size={11} className="mr-1" />
                            {lang === 'vi' ? 'Quản Lý Ngay' : 'Manage'}
                        </Badge>
                    ) : (
                        <a
                            href={botInviteUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            onClick={(e) => e.stopPropagation()}
                            className="inline-flex items-center gap-1.5 text-xs font-semibold text-[#ff2a85] hover:underline"
                        >
                            {lang === 'vi' ? 'Mời Bot Vào' : 'Add Bot'} <ExternalLink size={11} />
                        </a>
                    )}
                </div>
            </div>
        </Card>
    )
}
