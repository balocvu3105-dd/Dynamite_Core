import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui'
import { useLangStore } from '@/i18n'
import { useAuthStore } from '@/store/authStore'
import { discordOAuthUrl } from '@/lib/utils'
import {
  Wand2,
  Shield,
  Coins,
  Terminal,
  MessageSquare,
  Globe,
  Sparkles,
  Zap,
  Lock,
  ArrowRight,
  ExternalLink,
  Ban,
} from 'lucide-react'

export default function LandingPage() {
  const { lang, toggleLanguage } = useLangStore()
  const { user } = useAuthStore()
  const navigate = useNavigate()
  const [activeTab, setActiveTab] = useState<'features' | 'setup' | 'commands'>('features')

  const handleLoginOrDashboard = () => {
    if (user) {
      navigate('/servers')
    } else {
      window.location.href = discordOAuthUrl()
    }
  }

  const handleInviteBot = () => {
    // Standard Discord OAuth2 Bot Invite URL
    window.open(
      'https://discord.com/api/oauth2/authorize?client_id=1514609829610782862&permissions=8&scope=bot%20applications.commands',
      '_blank'
    )
  }

  return (
    <div className="min-h-screen bg-[--color-surface] text-[--color-text] flex flex-col selection:bg-[--color-brand] selection:text-white">
      {/* 1. Dyno-Style Navbar */}
      <header className="sticky top-0 z-50 h-16 border-b border-[--color-border] bg-[--color-surface]/90 backdrop-blur-md px-4 lg:px-12 flex items-center justify-between">
        <div className="flex items-center gap-8">
          {/* Logo */}
          <div
            onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
            className="flex items-center gap-2.5 cursor-pointer group"
          >
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-pink-500 to-[--color-brand] flex items-center justify-center shadow-lg shadow-[--color-brand]/30 group-hover:scale-105 transition-transform">
              <span className="text-white text-xl font-black tracking-tighter">D</span>
            </div>
            <div className="flex items-center gap-1.5 text-xl font-black tracking-tight">
              <span className="text-white">Dynamite</span>
              <span className="text-[--color-brand] drop-shadow-[0_0_10px_rgba(255,46,147,0.5)]">Core</span>
            </div>
          </div>

          {/* Nav Links */}
          <nav className="hidden md:flex items-center gap-6 text-sm font-semibold text-[--color-text-muted]">
            <a
              href="#features"
              onClick={() => setActiveTab('features')}
              className="hover:text-pink-400 transition-colors"
            >
              {lang === 'vi' ? 'Tính Năng' : 'Features'}
            </a>
            <a
              href="#setup"
              onClick={() => setActiveTab('setup')}
              className="hover:text-pink-400 transition-colors"
            >
              {lang === 'vi' ? 'Chuyên Gia Setup' : 'Smart Setup'}
            </a>
            <a
              href="#commands"
              onClick={() => setActiveTab('commands')}
              className="hover:text-pink-400 transition-colors"
            >
              {lang === 'vi' ? 'Danh Sách Lệnh' : 'Commands'}
            </a>
            <span className="flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 text-xs font-bold">
              <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
              {lang === 'vi' ? 'Hệ Thống 🟢 Online' : 'Status: Online'}
            </span>
          </nav>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center gap-3">
          <button
            onClick={toggleLanguage}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-[--color-border] bg-[--color-surface-alt] hover:border-[--color-brand] text-xs font-bold transition-all cursor-pointer"
          >
            <Globe size={14} className="text-[--color-brand]" />
            <span>{lang === 'vi' ? '🇻🇳 VI' : '🇬🇧 EN'}</span>
          </button>

          <Button
            variant="secondary"
            onClick={handleInviteBot}
            className="hidden sm:flex items-center gap-2 border border-[--color-border] bg-[#2b2d31] hover:bg-[#383a40] text-white font-bold text-xs px-4 py-2 rounded-lg cursor-pointer transition-all"
          >
            <span>+ {lang === 'vi' ? 'Thêm Vào Server' : 'Add To Server'}</span>
            <ExternalLink size={13} />
          </Button>

          <Button
            onClick={handleLoginOrDashboard}
            className="flex items-center gap-2 bg-[--color-brand] hover:bg-pink-600 text-white font-bold text-xs px-5 py-2 rounded-lg shadow-lg shadow-[--color-brand]/30 hover:shadow-[--color-brand]/50 transition-all transform hover:scale-105 cursor-pointer"
          >
            <span>{user ? (lang === 'vi' ? 'Quản Lý Máy Chủ' : 'Manage Servers') : (lang === 'vi' ? 'Đăng Nhập Dashboard' : 'Login / Manage')}</span>
            <ArrowRight size={14} />
          </Button>
        </div>
      </header>

      {/* 2. Hero Section */}
      <section className="relative pt-20 pb-28 px-4 text-center overflow-hidden">
        {/* Glow Effects */}
        <div className="absolute top-1/4 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[400px] bg-[--color-brand]/15 blur-[130px] rounded-full pointer-events-none" />

        <div className="relative z-10 max-w-4xl mx-auto space-y-6">
          <div className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-[--color-brand]/10 border border-[--color-brand]/30 text-pink-400 text-xs font-bold uppercase tracking-wider mb-2">
            <Sparkles size={14} />
            {lang === 'vi' ? 'Đã Nâng Cấp Giao Diện Đen Hồng Thế Hệ Mới' : 'Next-Gen Black & Pink Dashboard v1.0'}
          </div>

          <h1 className="text-4xl sm:text-6xl font-black tracking-tight leading-tight">
            {lang === 'vi' ? 'Quản Lý Máy Chủ Discord Của Bạn' : 'Supercharge Your Discord Server With'}{' '}
            <span className="text-white font-black">
              Dynamite <span className="text-[--color-brand] drop-shadow-[0_0_15px_rgba(255,46,147,0.6)]">Core</span>
            </span>
          </h1>

          <p className="text-base sm:text-lg text-[--color-text-muted] max-w-2xl mx-auto leading-relaxed">
            {lang === 'vi'
              ? 'Bot đa năng số #1 với hệ thống Kiểm duyệt tự động thông minh, Kinh tế & Câu cá V2, Cổng xác thực Lời chào và Chuyên gia tự động thiết lập máy chủ chỉ với 1 cú nhấp chuột!'
              : 'The ultimate all-in-one Discord bot equipped with Auto-Moderation, Economy V2, Smart Setup Engine, and Live Discord Message Simulation.'}
          </p>

          <div className="flex flex-col sm:flex-row items-center justify-center gap-4 pt-4">
            <Button
              size="lg"
              onClick={handleInviteBot}
              className="w-full sm:w-auto px-8 py-3.5 rounded-xl bg-[--color-brand] hover:bg-pink-600 text-white font-extrabold shadow-xl shadow-[--color-brand]/40 transform hover:scale-105 transition-all flex items-center justify-center gap-2 cursor-pointer"
            >
              <span>🚀 {lang === 'vi' ? 'Thêm Dynamite Ngay' : 'Add To Your Server'}</span>
            </Button>
            <Button
              size="lg"
              variant="secondary"
              onClick={handleLoginOrDashboard}
              className="w-full sm:w-auto px-8 py-3.5 rounded-xl bg-[--color-surface-alt] hover:bg-[--color-surface-raised] border border-[--color-border] text-[--color-text] font-extrabold flex items-center justify-center gap-2 cursor-pointer transition-all"
            >
              <span>⚡ {lang === 'vi' ? 'Mở Bảng Điều Khiển' : 'Open Dashboard'}</span>
              <ArrowRight size={16} />
            </Button>
          </div>

          {/* Stats Bar */}
          <div className="pt-12 grid grid-cols-3 gap-6 max-w-2xl mx-auto border-t border-[--color-border]/50 text-center">
            <div>
              <div className="text-2xl sm:text-3xl font-black text-white">100%</div>
              <div className="text-xs text-[--color-text-muted] mt-1">{lang === 'vi' ? 'Miễn Phí & Song Ngữ' : 'Free & Bilingual'}</div>
            </div>
            <div>
              <div className="text-2xl sm:text-3xl font-black text-pink-400">15+</div>
              <div className="text-xs text-[--color-text-muted] mt-1">{lang === 'vi' ? 'Lệnh & Mô-đun' : 'Commands & Modules'}</div>
            </div>
            <div>
              <div className="text-2xl sm:text-3xl font-black text-purple-400">99.9%</div>
              <div className="text-xs text-[--color-text-muted] mt-1">{lang === 'vi' ? 'Thời Gian Hoạt Động' : 'Uptime Guarantee'}</div>
            </div>
          </div>
        </div>
      </section>

      {/* 3. Interactive Section Tabs */}
      <section className="max-w-6xl mx-auto px-4 pb-24 w-full space-y-8">
        <div className="flex justify-center border-b border-[--color-border]">
          <div className="flex gap-4">
            {[
              { id: 'features', labelVi: '🔥 Tính Năng Nổi Bật', labelEn: '🔥 Core Features' },
              { id: 'setup', labelVi: '✨ Chuyên Gia Setup Tự Động', labelEn: '✨ Smart Setup Engine' },
              { id: 'commands', labelVi: '⚡ Cú Pháp Lệnh Nhanh', labelEn: '⚡ Slash Commands' },
            ].map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id as any)}
                className={`pb-4 px-4 font-bold text-sm transition-all border-b-2 cursor-pointer ${
                  activeTab === tab.id
                    ? 'border-[--color-brand] text-[--color-brand]'
                    : 'border-transparent text-[--color-text-muted] hover:text-[--color-text]'
                }`}
              >
                {lang === 'vi' ? tab.labelVi : tab.labelEn}
              </button>
            ))}
          </div>
        </div>

        {/* Tab Content 1: Core Features */}
        {activeTab === 'features' && (
          <div id="features" className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="p-6 rounded-2xl bg-[--color-surface-alt] border border-[--color-border] hover:border-[--color-brand]/50 transition-all space-y-4 shadow-lg group">
              <div className="w-12 h-12 rounded-xl bg-pink-500/10 border border-pink-500/20 flex items-center justify-center text-[--color-brand] group-hover:scale-110 transition-transform">
                <Shield size={24} />
              </div>
              <h3 className="text-lg font-bold text-[--color-text]">
                {lang === 'vi' ? 'Kiểm Duyệt Tự Động (AutoMod)' : 'Advanced Moderation'}
              </h3>
              <p className="text-sm text-[--color-text-muted] leading-relaxed">
                {lang === 'vi'
                  ? 'Bảo vệ máy chủ 24/7 trước Spam lặp lại, Raid hàng loạt, link lừa đảo và tự động cấm ngôn (Timeout) theo quy tắc bạn định sẵn.'
                  : 'Protect your server 24/7 against spam, raids, phishing links, and auto-timeout offenders based on customizable rules.'}
              </p>
            </div>

            <div className="p-6 rounded-2xl bg-[--color-surface-alt] border border-[--color-border] hover:border-[--color-brand]/50 transition-all space-y-4 shadow-lg group">
              <div className="w-12 h-12 rounded-xl bg-purple-500/10 border border-purple-500/20 flex items-center justify-center text-purple-400 group-hover:scale-110 transition-transform">
                <Coins size={24} />
              </div>
              <h3 className="text-lg font-bold text-[--color-text]">
                {lang === 'vi' ? 'Kinh Tế V2 & Câu Cá Hiếm' : 'Economy V2 & Fishing'}
              </h3>
              <p className="text-sm text-[--color-text-muted] leading-relaxed">
                {lang === 'vi'
                  ? 'Hệ thống tiền tệ hoàn chỉnh: điểm danh nhận thưởng mỗi ngày (/daily), quay máy may mắn (/slots) và câu cá huyền thoại (/fish).'
                  : 'Full currency ecosystem: daily rewards, slot machine jackpots, and rare/legendary fishing mini-games to boost engagement.'}
              </p>
            </div>

            <div className="p-6 rounded-2xl bg-[--color-surface-alt] border border-[--color-border] hover:border-[--color-brand]/50 transition-all space-y-4 shadow-lg group">
              <div className="w-12 h-12 rounded-xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center text-emerald-400 group-hover:scale-110 transition-transform">
                <MessageSquare size={24} />
              </div>
              <h3 className="text-lg font-bold text-[--color-text]">
                {lang === 'vi' ? 'Xem Trước Chat Trực Tiếp' : 'Live Discord Preview'}
              </h3>
              <p className="text-sm text-[--color-text-muted] leading-relaxed">
                {lang === 'vi'
                  ? 'Chỉnh sửa lời chào mừng thành viên mới và xem trước trực tiếp ngay trên nền tối Discord giả lập ngay trong Web Dashboard.'
                  : 'Customize welcome messages and see real-time simulated Discord dark-mode output right in your dashboard before saving.'}
              </p>
            </div>

            <div className="p-6 rounded-2xl bg-[--color-surface-alt] border border-[--color-border] hover:border-[--color-brand]/50 transition-all space-y-4 shadow-lg group">
              <div className="w-12 h-12 rounded-xl bg-amber-500/10 border border-amber-500/20 flex items-center justify-center text-amber-400 group-hover:scale-110 transition-transform">
                <Ban size={24} />
              </div>
              <h3 className="text-lg font-bold text-[--color-text]">
                {lang === 'vi' ? 'Danh Sách Cấm & Bỏ Qua' : 'Blacklist & Ignore Rules'}
              </h3>
              <p className="text-sm text-[--color-text-muted] leading-relaxed">
                {lang === 'vi'
                  ? 'Chỉ định chính xác các kênh thảo luận riêng (#rules, #staff) hoặc vai trò VIP được miễn trừ khỏi kiểm duyệt hay lệnh của Bot.'
                  : 'Precise exclusion control: designate ignored channels or bypass roles where bot commands and automod checks are disabled.'}
              </p>
            </div>

            <div className="p-6 rounded-2xl bg-[--color-surface-alt] border border-[--color-border] hover:border-[--color-brand]/50 transition-all space-y-4 shadow-lg group">
              <div className="w-12 h-12 rounded-xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center text-blue-400 group-hover:scale-110 transition-transform">
                <Zap size={24} />
              </div>
              <h3 className="text-lg font-bold text-[--color-text]">
                {lang === 'vi' ? 'Giao Diện Đen - Hồng' : 'Black & Pink Aesthetic'}
              </h3>
              <p className="text-sm text-[--color-text-muted] leading-relaxed">
                {lang === 'vi'
                  ? 'Trải nghiệm bảng điều khiển siêu mượt mà với hiệu ứng viền trắng động, màu neon hồng nổi bật cực kỳ hiện đại và đẳng cấp.'
                  : 'Sleek dark mode obsidian styling accented with vibrant neon pink and interactive white border glow animations.'}
              </p>
            </div>

            <div className="p-6 rounded-2xl bg-[--color-surface-alt] border border-[--color-border] hover:border-[--color-brand]/50 transition-all space-y-4 shadow-lg group">
              <div className="w-12 h-12 rounded-xl bg-rose-500/10 border border-rose-500/20 flex items-center justify-center text-rose-400 group-hover:scale-110 transition-transform">
                <Lock size={24} />
              </div>
              <h3 className="text-lg font-bold text-[--color-text]">
                {lang === 'vi' ? 'Bảo Mật & Cổng Xác Thực' : 'Security Gate & Audit Logs'}
              </h3>
              <p className="text-sm text-[--color-text-muted] leading-relaxed">
                {lang === 'vi'
                  ? 'Yêu cầu thành viên mới bấm nút xác thực để chống tài khoản giả mạo (Self-Bot), đồng thời ghi nhật ký mọi sự kiện máy chủ.'
                  : 'Require verification gates to prevent self-bot raids and maintain full transparency with detailed channel audit logs.'}
              </p>
            </div>
          </div>
        )}

        {/* Tab Content 2: Smart Setup */}
        {activeTab === 'setup' && (
          <div id="setup" className="bg-[--color-surface-alt] border border-[--color-border] rounded-2xl p-8 space-y-6">
            <div className="flex items-center gap-3">
              <Wand2 className="text-[--color-brand]" size={28} />
              <div>
                <h3 className="text-xl font-bold text-[--color-text]">
                  {lang === 'vi' ? 'Chuyên Gia Tự Động Thiết Lập Máy Chủ (Smart Setup Engine)' : 'Smart Setup Engine'}
                </h3>
                <p className="text-sm text-[--color-text-muted]">
                  {lang === 'vi' ? 'Tự động xây dựng kênh, vai trò và phân quyền chỉ trong 3 giây' : 'Instantly generate channels, roles, and permissions in 3 seconds'}
                </p>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6 pt-4">
              <div className="p-5 rounded-xl bg-[--color-surface] border border-[--color-border] space-y-3">
                <div className="text-base font-bold text-pink-400">🎮 {lang === 'vi' ? 'Mẫu Server Game Thủ' : 'Gaming Preset'}</div>
                <p className="text-xs text-[--color-text-muted] leading-relaxed">
                  {lang === 'vi' ? 'Tạo sẵn danh mục Tìm Tổ Đội (LFG), Kênh Voice Squad 1-4, Góc Highlight Clip và Role Game Thủ.' : 'Creates LFG lobby, Voice Squads 1-4, Highlight clips channel and Gaming role hierarchy.'}
                </p>
                <div className="font-mono text-xs text-emerald-400 bg-black/40 p-2 rounded">/setup gaming</div>
              </div>

              <div className="p-5 rounded-xl bg-[--color-surface] border border-[--color-border] space-y-3">
                <div className="text-base font-bold text-purple-400">🌐 {lang === 'vi' ? 'Mẫu Cộng Đồng Trò Chuyện' : 'Community Preset'}</div>
                <p className="text-xs text-[--color-text-muted] leading-relaxed">
                  {lang === 'vi' ? 'Tạo sẵn sảnh Welcome, Phòng Trò Chuyện Chung, Góc Meme, Kênh Âm Nhạc và Vai Trò Member.' : 'Creates Welcome lounge, General chat, Meme corner, Music rooms and Member roles.'}
                </p>
                <div className="font-mono text-xs text-emerald-400 bg-black/40 p-2 rounded">/setup community</div>
              </div>

              <div className="p-5 rounded-xl bg-[--color-surface] border border-[--color-border] space-y-3">
                <div className="text-base font-bold text-amber-400">📺 {lang === 'vi' ? 'Mẫu Streamer / Creator' : 'Streamer Preset'}</div>
                <p className="text-xs text-[--color-text-muted] leading-relaxed">
                  {lang === 'vi' ? 'Tạo sẵn Kênh Thông Báo Live Stream, Phòng Sub Lounge VIP, Góc Fan Art và Role Subscriber.' : 'Creates Stream announcements, VIP Sub Lounge, Fan Art corner and Subscriber roles.'}
                </p>
                <div className="font-mono text-xs text-emerald-400 bg-black/40 p-2 rounded">/setup streamer</div>
              </div>
            </div>
          </div>
        )}

        {/* Tab Content 3: Commands Preview */}
        {activeTab === 'commands' && (
          <div id="commands" className="bg-[--color-surface-alt] border border-[--color-border] rounded-2xl p-8 space-y-6">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <Terminal className="text-[--color-brand]" size={28} />
                <div>
                  <h3 className="text-xl font-bold text-[--color-text]">
                    {lang === 'vi' ? 'Một Số Lệnh Tiêu Biểu Của Dynamite' : 'Featured Slash Commands'}
                  </h3>
                  <p className="text-sm text-[--color-text-muted]">
                    {lang === 'vi' ? 'Hỗ trợ gợi ý cú pháp tự động 100% trên Discord' : '100% autocompleted slash commands on Discord'}
                  </p>
                </div>
              </div>
              <Button onClick={handleLoginOrDashboard} className="text-xs bg-[--color-brand] cursor-pointer">
                {lang === 'vi' ? 'Xem Toàn Bộ 15+ Lệnh' : 'View All Commands'}
              </Button>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {[
                { cmd: '/ban @user [lý do]', descVi: 'Cấm thành viên vi phạm khỏi máy chủ', descEn: 'Ban offending member from server' },
                { cmd: '/mute @user 10m', descVi: 'Cấm ngôn (Timeout) tạm thời trong 10 phút', descEn: 'Timeout user for 10 minutes' },
                { cmd: '/fish', descVi: 'Câu cá nhận xu vàng, có cơ hội bắt cá truyền thuyết', descEn: 'Catch fish and earn currency' },
                { cmd: '/balance', descVi: 'Xem số dư ví và tài khoản ngân hàng', descEn: 'Check coin balance and net worth' },
                { cmd: '/setup gaming', descVi: 'Tự động tạo toàn bộ kênh cho server game', descEn: 'Auto-build channels for gaming server' },
                { cmd: '/clear 50', descVi: 'Dọn dẹp 50 tin nhắn gần nhất trong kênh', descEn: 'Purge last 50 messages in channel' },
              ].map((item, idx) => (
                <div key={idx} className="p-3.5 rounded-lg bg-[--color-surface] border border-[--color-border] flex items-center justify-between">
                  <span className="font-mono text-sm font-bold text-pink-400">{item.cmd}</span>
                  <span className="text-xs text-[--color-text-muted]">{lang === 'vi' ? item.descVi : item.descEn}</span>
                </div>
              ))}
            </div>
          </div>
        )}
      </section>

      {/* 4. Footer */}
      <footer className="mt-auto border-t border-[--color-border] bg-[--color-surface-alt] py-12 px-4 text-center text-sm text-[--color-text-muted]">
        <div className="max-w-6xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-6">
          <div className="flex items-center gap-2 font-bold text-[--color-text]">
            <div className="w-6 h-6 rounded-md bg-[--color-brand] flex items-center justify-center text-white text-xs">D</div>
            <span>Dynamite Core © 2026</span>
          </div>
          <div className="flex gap-6 text-xs font-semibold">
            <a href="#features" className="hover:text-pink-400 transition-colors">{lang === 'vi' ? 'Tính năng' : 'Features'}</a>
            <a href="#setup" className="hover:text-pink-400 transition-colors">{lang === 'vi' ? 'Thiết lập' : 'Setup'}</a>
            <a href="#commands" className="hover:text-pink-400 transition-colors">{lang === 'vi' ? 'Lệnh' : 'Commands'}</a>
          </div>
        </div>
      </footer>
    </div>
  )
}
