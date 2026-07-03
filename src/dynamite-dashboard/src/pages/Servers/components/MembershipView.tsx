import { Crown, CheckCircle2, Zap, Shield, Sparkles } from 'lucide-react'
import { Card, Badge, Button } from '@/components/ui'
import type { UserProfile } from '@/types'

interface MembershipViewProps {
    user: UserProfile
    lang: string
}

export function MembershipView({ user, lang }: MembershipViewProps) {
    const isVip = user.username.toLowerCase().includes('dynamite') || user.id === '999999999999999999'

    return (
        <div className="space-y-6 animate-fadeIn">
            <Card className="p-6 sm:p-8 border-amber-500/30 bg-gradient-to-r from-amber-500/10 via-[--color-surface-raised] to-[#ff2a85]/10">
                <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-6">
                    <div className="flex items-center gap-4">
                        <div className="w-14 h-14 rounded-2xl bg-gradient-to-tr from-amber-500 to-[#ff2a85] flex items-center justify-center text-white shadow-lg">
                            <Crown size={28} />
                        </div>
                        <div>
                            <div className="flex items-center gap-2">
                                <h2 className="text-xl sm:text-2xl font-bold text-[--color-text]">
                                    {isVip ? 'Dynamite Core VIP Pro' : 'Dynamite Core Free Member'}
                                </h2>
                                <Badge variant={isVip ? 'warning' : 'default'}>
                                    {isVip ? 'VIP TIER' : 'STANDARD'}
                                </Badge>
                            </div>
                            <p className="text-sm text-[--color-text-muted] mt-1">
                                {lang === 'vi'
                                    ? 'Trạng thái quyền lợi tài khoản Discord quản trị viên'
                                    : 'Discord Administrator account privilege status'}
                            </p>
                        </div>
                    </div>
                    <Button
                        className="bg-gradient-to-r from-[#ff2a85] to-amber-500 text-white font-bold px-6 shadow-md hover:opacity-90 transition-opacity"
                    >
                        <Sparkles size={16} className="mr-2" />
                        {lang === 'vi' ? 'Nâng Cấp VIP Ngay' : 'Upgrade Membership'}
                    </Button>
                </div>
            </Card>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <Card className="p-5 space-y-3">
                    <div className="flex items-center gap-2.5 text-[#ff2a85] font-bold">
                        <Shield size={20} />
                        <h3>{lang === 'vi' ? 'Real-time Moderation' : 'Real-time Moderation'}</h3>
                    </div>
                    <p className="text-sm text-[--color-text-muted]">
                        {lang === 'vi'
                            ? 'Lịch sử cảnh cáo và vi phạm được cập nhật lập tức mỗi 1.5s kèm tên hiển thị chính xác.'
                            : 'Violation logs and warnings synced live every 1.5s with exact display names.'}
                    </p>
                    <div className="flex items-center gap-1.5 text-xs text-emerald-400 font-semibold">
                        <CheckCircle2 size={14} /> {lang === 'vi' ? 'Đã kích hoạt' : 'Active'}
                    </div>
                </Card>

                <Card className="p-5 space-y-3">
                    <div className="flex items-center gap-2.5 text-amber-500 font-bold">
                        <Zap size={20} />
                        <h3>{lang === 'vi' ? 'Auto Blacklist Defense' : 'Auto Blacklist Defense'}</h3>
                    </div>
                    <p className="text-sm text-[--color-text-muted]">
                        {lang === 'vi'
                            ? 'Tự động khóa cấm (ban) vĩnh viễn các đối tượng độc hại có trong danh sách đen toàn hệ thống.'
                            : 'Automated instant re-ban defense against malicious blacklisted offenders across servers.'}
                    </p>
                    <div className="flex items-center gap-1.5 text-xs text-emerald-400 font-semibold">
                        <CheckCircle2 size={14} /> {lang === 'vi' ? 'Đã kích hoạt' : 'Active'}
                    </div>
                </Card>

                <Card className="p-5 space-y-3">
                    <div className="flex items-center gap-2.5 text-indigo-400 font-bold">
                        <Crown size={20} />
                        <h3>{lang === 'vi' ? 'Hỗ Trợ Ưu Tiên 24/7' : 'Priority 24/7 Support'}</h3>
                    </div>
                    <p className="text-sm text-[--color-text-muted]">
                        {lang === 'vi'
                            ? 'Đội ngũ phát triển Dynamite Core hỗ trợ kỹ thuật trực tiếp và tuỳ chỉnh theo yêu cầu.'
                            : 'Direct technical assistance and tailored setup guidelines from Dynamite Core developers.'}
                    </p>
                    <div className="flex items-center gap-1.5 text-xs text-emerald-400 font-semibold">
                        <CheckCircle2 size={14} /> {lang === 'vi' ? 'Đã kích hoạt' : 'Active'}
                    </div>
                </Card>
            </div>
        </div>
    )
}
