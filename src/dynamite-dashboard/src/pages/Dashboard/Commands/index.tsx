import { useState } from 'react'
import { Card, Badge, Toggle } from '@/components/ui'
import { useLangStore } from '@/i18n'
import { Terminal, Search, CheckCircle2 } from 'lucide-react'

interface CommandItem {
  name: string
  category: 'moderation' | 'economy' | 'utility' | 'setup'
  descVi: string
  descEn: string
  usage: string
  adminOnly?: boolean
}

const COMMANDS_DATA: CommandItem[] = [
  { name: '/ban', category: 'moderation', descVi: 'Cấm một thành viên khỏi máy chủ vĩnh viễn hoặc có thời hạn.', descEn: 'Ban a member from the server permanently or temporarily.', usage: '/ban @user [lý do]', adminOnly: true },
  { name: '/kick', category: 'moderation', descVi: 'Đuổi một thành viên khỏi máy chủ.', descEn: 'Kick a member from the server.', usage: '/kick @user [lý do]', adminOnly: true },
  { name: '/mute', category: 'moderation', descVi: 'Cấm ngôn (Timeout) thành viên trong một khoảng thời gian.', descEn: 'Timeout/mute a member for a specified duration.', usage: '/mute @user 10m [lý do]', adminOnly: true },
  { name: '/unmute', category: 'moderation', descVi: 'Gỡ cấm ngôn cho thành viên.', descEn: 'Remove timeout from a member.', usage: '/unmute @user', adminOnly: true },
  { name: '/warn', category: 'moderation', descVi: 'Gửi cảnh báo chính thức và ghi vào nhật ký vi phạm.', descEn: 'Issue a formal warning and log it.', usage: '/warn @user [lý do]', adminOnly: true },
  { name: '/clear', category: 'moderation', descVi: 'Dọn dẹp hàng loạt tin nhắn trong kênh chat.', descEn: 'Bulk delete messages in a channel.', usage: '/clear 50', adminOnly: true },
  { name: '/setup gaming', category: 'setup', descVi: 'Tự động tạo bộ kênh LFG, Voice Squad và Role cho game thủ.', descEn: 'Auto create LFG channels, voice squads and roles for gaming.', usage: '/setup gaming', adminOnly: true },
  { name: '/setup community', category: 'setup', descVi: 'Tự động tạo sảnh trò chuyện chung, kênh meme và thảo luận.', descEn: 'Auto create general lounges, memes and discussion channels.', usage: '/setup community', adminOnly: true },
  { name: '/setup streamer', category: 'setup', descVi: 'Tự động tạo thông báo live stream, góc fan art và phòng Sub.', descEn: 'Auto create stream notifications, fan art and sub lounges.', usage: '/setup streamer', adminOnly: true },
  { name: '/fish', category: 'economy', descVi: 'Câu cá kiếm xu, có tỷ lệ ra cá hiếm và cá huyền thoại.', descEn: 'Fish to earn coins with chances for rare and legendary catches.', usage: '/fish' },
  { name: '/balance', category: 'economy', descVi: 'Xem số dư ví và tài khoản ngân hàng của bạn.', descEn: 'Check your wallet and bank balance.', usage: '/balance [@user]' },
  { name: '/daily', category: 'economy', descVi: 'Nhận phần thưởng xu điểm danh mỗi ngày.', descEn: 'Claim your daily coin reward.', usage: '/daily' },
  { name: '/slots', category: 'economy', descVi: 'Chơi máy quay số jackpot thử vận may.', descEn: 'Play slot machine to win jackpot.', usage: '/slots [số tiền]' },
  { name: '/ping', category: 'utility', descVi: 'Kiểm tra độ trễ phản hồi của Bot và kết nối Discord.', descEn: 'Check bot latency and Discord API response time.', usage: '/ping' },
  { name: '/help', category: 'utility', descVi: 'Hiển thị menu hướng dẫn toàn bộ lệnh của Dynamite.', descEn: 'Display interactive help menu for all Dynamite commands.', usage: '/help' },
]

export default function CommandsPage() {
  const { lang } = useLangStore()
  const [search, setSearch] = useState('')
  const [selectedCat, setSelectedCat] = useState<string>('all')
  const [enabledCommands, setEnabledCommands] = useState<Record<string, boolean>>(
    COMMANDS_DATA.reduce((acc, cmd) => ({ ...acc, [cmd.name]: true }), {})
  )

  const toggleCommand = (name: string) => {
    setEnabledCommands((prev) => ({ ...prev, [name]: !prev[name] }))
  }

  const filtered = COMMANDS_DATA.filter((cmd) => {
    const matchesCat = selectedCat === 'all' || cmd.category === selectedCat
    const matchesQuery =
      cmd.name.toLowerCase().includes(search.toLowerCase()) ||
      cmd.descVi.toLowerCase().includes(search.toLowerCase()) ||
      cmd.descEn.toLowerCase().includes(search.toLowerCase())
    return matchesCat && matchesQuery
  })

  return (
    <div className="max-w-5xl space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-bold text-[--color-text] flex items-center gap-2.5">
            <Terminal className="text-[--color-brand]" />
            {lang === 'vi' ? 'Quản Lý Lệnh Slash Commands' : 'Slash Commands Management'}
          </h2>
          <p className="text-sm text-[--color-text-muted] mt-1">
            {lang === 'vi'
              ? 'Bật/tắt và phân quyền các lệnh tự động trên máy chủ của bạn'
              : 'Toggle and configure commands available on your Discord server'}
          </p>
        </div>
        <div className="flex items-center gap-2 bg-[--color-surface-raised] px-3 py-1.5 rounded-lg border border-[--color-border]">
          <CheckCircle2 size={16} className="text-emerald-400" />
          <span className="text-xs font-semibold text-[--color-text]">
            {Object.values(enabledCommands).filter(Boolean).length} / {COMMANDS_DATA.length}{' '}
            {lang === 'vi' ? 'Lệnh Đang Bật' : 'Enabled'}
          </span>
        </div>
      </div>

      {/* Search & Filter Bar */}
      <Card className="p-4 bg-[--color-surface] border-[--color-border] flex flex-col sm:flex-row gap-4 items-center justify-between">
        <div className="relative w-full sm:w-80">
          <Search size={16} className="absolute left-3 top-2.5 text-[--color-text-muted]" />
          <input
            type="text"
            placeholder={lang === 'vi' ? 'Tìm kiếm lệnh (/ban, /fish)...' : 'Search commands...'}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-9 pr-4 py-1.5 rounded-md bg-[--color-surface-alt] border border-[--color-border] text-sm text-[--color-text] focus:outline-none focus:ring-2 focus:ring-[--color-brand]"
          />
        </div>

        <div className="flex flex-wrap gap-2 w-full sm:w-auto">
          {[
            { id: 'all', labelVi: 'Tất Cả', labelEn: 'All Categories' },
            { id: 'moderation', labelVi: '🛡️ Quản Trị', labelEn: 'Moderation' },
            { id: 'setup', labelVi: '✨ Thiết Lập', labelEn: 'Setup' },
            { id: 'economy', labelVi: '🎰 Kinh Tế', labelEn: 'Economy' },
            { id: 'utility', labelVi: '🔧 Tiện Ích', labelEn: 'Utility' },
          ].map((cat) => (
            <button
              key={cat.id}
              onClick={() => setSelectedCat(cat.id)}
              className={`px-4 py-2 rounded-lg text-xs font-bold transition-all duration-300 transform active:scale-95 cursor-pointer relative ${
                selectedCat === cat.id
                  ? 'bg-[--color-brand] text-white ring-2 ring-white ring-offset-2 ring-offset-[--color-surface] shadow-xl shadow-[--color-brand]/40 scale-105'
                  : 'bg-[--color-surface-alt] text-[--color-text-muted] hover:text-[--color-text] hover:bg-[--color-surface-raised]'
              }`}
            >
              {lang === 'vi' ? cat.labelVi : cat.labelEn}
            </button>
          ))}
        </div>
      </Card>

      {/* Commands Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {filtered.map((cmd) => (
          <Card
            key={cmd.name}
            className={`p-4 transition-all duration-200 border ${
              enabledCommands[cmd.name]
                ? 'border-[--color-border] hover:border-[--color-brand]/60 bg-[--color-surface-alt]'
                : 'border-[--color-border]/50 opacity-60 bg-[--color-surface]'
            }`}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <span className="font-mono font-bold text-base text-[--color-brand] bg-[--color-brand]/10 px-2 py-0.5 rounded border border-[--color-brand]/20">
                    {cmd.name}
                  </span>
                  {cmd.adminOnly ? (
                    <Badge variant="danger">Admin Only</Badge>
                  ) : (
                    <Badge variant="success">Everyone</Badge>
                  )}
                </div>
                <p className="text-xs text-[--color-text] leading-relaxed pt-1">
                  {lang === 'vi' ? cmd.descVi : cmd.descEn}
                </p>
                <div className="pt-2 flex items-center gap-2 text-[11px] font-mono text-[--color-text-muted]">
                  <span>Cú pháp:</span>
                  <code className="bg-[--color-surface] px-1.5 py-0.5 rounded text-pink-300">
                    {cmd.usage}
                  </code>
                </div>
              </div>

              <Toggle
                checked={enabledCommands[cmd.name] ?? true}
                onChange={() => toggleCommand(cmd.name)}
              />
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}
