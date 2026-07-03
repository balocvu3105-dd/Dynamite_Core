import { Globe, LogOut, ShieldAlert, Key } from 'lucide-react'
import { Card, Button } from '@/components/ui'
import type { UserProfile } from '@/types'

interface UserSettingsViewProps {
    user: UserProfile
    lang: string
    onToggleLang: () => void
    onLogout: () => void
}

export function UserSettingsView({ user, lang, onToggleLang, onLogout }: UserSettingsViewProps) {
    return (
        <div className="space-y-6 animate-fadeIn max-w-3xl">
            <Card className="p-6 space-y-6">
                <div>
                    <h3 className="text-lg font-bold text-[--color-text]">
                        {lang === 'vi' ? 'Cài Đặt Ngôn Ngữ Giao Diện' : 'Interface Language Settings'}
                    </h3>
                    <p className="text-sm text-[--color-text-muted] mt-1">
                        {lang === 'vi'
                            ? 'Lựa chọn ngôn ngữ hiển thị chính trên toàn bộ Web Dashboard Dynamite Core.'
                            : 'Choose primary display language across the Dynamite Core Web Dashboard.'}
                    </p>
                </div>
                <div className="flex items-center justify-between p-4 rounded-xl bg-[--color-surface] border border-[--color-border]">
                    <div className="flex items-center gap-3">
                        <Globe className="text-[#ff2a85]" size={22} />
                        <div>
                            <p className="font-semibold text-sm text-[--color-text]">
                                {lang === 'vi' ? 'Ngôn ngữ hiện tại: Tiếng Việt (VN)' : 'Current Language: English (US)'}
                            </p>
                        </div>
                    </div>
                    <Button variant="secondary" onClick={onToggleLang} className="cursor-pointer">
                        {lang === 'vi' ? 'Chuyển sang English' : 'Switch to Tiếng Việt'}
                    </Button>
                </div>
            </Card>

            <Card className="p-6 space-y-6">
                <div>
                    <h3 className="text-lg font-bold text-[--color-text]">
                        {lang === 'vi' ? 'Thông Tin Tài Khoản Discord' : 'Discord Account Information'}
                    </h3>
                    <p className="text-sm text-[--color-text-muted] mt-1">
                        {lang === 'vi'
                            ? 'Tài khoản Discord đang liên kết để cấp quyền quản trị máy chủ.'
                            : 'Linked Discord profile providing server management authorization.'}
                    </p>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
                    <div className="p-3.5 rounded-lg bg-[--color-surface] border border-[--color-border]">
                        <span className="text-[--color-text-muted] text-xs block uppercase font-semibold">Discord User ID</span>
                        <span className="font-mono font-bold text-[--color-text] mt-1 block">{user.id}</span>
                    </div>
                    <div className="p-3.5 rounded-lg bg-[--color-surface] border border-[--color-border]">
                        <span className="text-[--color-text-muted] text-xs block uppercase font-semibold">Username</span>
                        <span className="font-bold text-[#ff2a85] mt-1 block">@{user.username}</span>
                    </div>
                </div>
            </Card>

            <Card className="p-6 border-[--color-danger]/30 bg-[--color-danger]/5 space-y-4">
                <div className="flex items-center gap-3 text-[--color-danger]">
                    <ShieldAlert size={22} />
                    <h3 className="text-lg font-bold">
                        {lang === 'vi' ? 'Đăng Xuất Phiên Làm Việc' : 'End Session'}
                    </h3>
                </div>
                <p className="text-sm text-[--color-text-muted]">
                    {lang === 'vi'
                            ? 'Đăng xuất tài khoản Discord khỏi trình duyệt hiện tại. Bạn sẽ cần xác thực lại OAuth2 ở lần đăng nhập tới.'
                            : 'Sign out your Discord account from this browser session.'}
                </p>
                <div className="pt-2">
                    <Button variant="danger" onClick={onLogout} className="flex items-center gap-2 cursor-pointer shadow-md">
                        <LogOut size={16} />
                        {lang === 'vi' ? 'Đăng Xuất Khỏi Hệ Thống' : 'Log Out Now'}
                    </Button>
                </div>
            </Card>
        </div>
    )
}
