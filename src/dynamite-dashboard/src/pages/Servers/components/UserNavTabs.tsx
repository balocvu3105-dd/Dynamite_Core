import { Users, UserCheck, Settings } from 'lucide-react'

export type NavTabType = 'servers' | 'membership' | 'settings'

interface UserNavTabsProps {
    activeTab: NavTabType
    onTabChange: (tab: NavTabType) => void
    lang: string
}

export function UserNavTabs({ activeTab, onTabChange, lang }: UserNavTabsProps) {
    const tabs = [
        {
            id: 'servers' as NavTabType,
            label: lang === 'vi' ? 'Máy Chủ' : 'Servers',
            icon: Users,
            colorClass: 'text-indigo-400',
        },
        {
            id: 'membership' as NavTabType,
            label: lang === 'vi' ? 'Tài Khoản & Gói' : 'Membership',
            icon: UserCheck,
            colorClass: 'text-amber-500 font-bold',
        },
        {
            id: 'settings' as NavTabType,
            label: lang === 'vi' ? 'Cài Đặt' : 'Settings',
            icon: Settings,
            colorClass: 'text-emerald-400',
        },
    ]

    return (
        <div className="grid grid-cols-3 gap-2.5 my-6 bg-[--color-surface-alt]/50 p-1.5 rounded-xl border border-[--color-border]">
            {tabs.map((tab) => {
                const Icon = tab.icon
                const isActive = activeTab === tab.id
                return (
                    <button
                        key={tab.id}
                        onClick={() => onTabChange(tab.id)}
                        className={`flex items-center justify-center gap-2.5 py-3.5 px-4 rounded-lg font-semibold text-sm transition-all cursor-pointer ${
                            isActive
                                ? 'bg-[--color-surface-raised] border-b-2 border-[#ff2a85] shadow-lg text-[--color-text]'
                                : 'bg-[--color-surface]/60 hover:bg-[--color-surface-hover] text-[--color-text-muted] hover:text-[--color-text]'
                        }`}
                    >
                        <Icon size={18} className={isActive ? tab.colorClass : 'opacity-70'} />
                        <span className={isActive && tab.id === 'membership' ? 'text-amber-500 font-bold' : ''}>
                            {tab.label}
                        </span>
                    </button>
                )
            })}
        </div>
    )
}
