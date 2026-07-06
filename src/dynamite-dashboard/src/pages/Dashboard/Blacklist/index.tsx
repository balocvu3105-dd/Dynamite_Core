import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Card, Button, Badge, Spinner } from '@/components/ui'
import { useLangStore } from '@/i18n'
import { Ban, UserX, Trash2, ShieldOff, Clock, Search } from 'lucide-react'
import { useToast } from '@/hooks/useToast'
import { useConfirm } from '@/hooks/useConfirm'
import { blacklistApi, type BlacklistEntry } from '@/api/blacklist'
import { formatDate } from '@/lib/utils'

export default function BlacklistPage() {
  const { guildId } = useParams<{ guildId: string }>()
  const { lang } = useLangStore()
  const toast = useToast()
  const confirm = useConfirm()
  const qc = useQueryClient()

  // Add form state
  const [newUserId, setNewUserId] = useState('')
  const [newUserName, setNewUserName] = useState('')
  const [newUserReason, setNewUserReason] = useState('')
  const [newUserNotes, setNewUserNotes] = useState('')
  const [removeReason, setRemoveReason] = useState('')
  const [search, setSearch] = useState('')
  const [targetAvatarUrl, setTargetAvatarUrl] = useState<string | null>(null)
  const [isLookingUp, setIsLookingUp] = useState(false)

  // ── Auto lookup Discord user when ID is entered ────────────────────────────
  useQuery({
    queryKey: ['lookupUser', guildId, newUserId.trim()],
    queryFn: async () => {
      setIsLookingUp(true)
      try {
        const user = await blacklistApi.lookupUser(guildId!, newUserId.trim())
        if (user) {
          setNewUserName(user.username)
          setTargetAvatarUrl(user.avatar)
          toast.success(lang === 'vi' ? `Đã tìm thấy: ${user.username}` : `Found user: ${user.username}`)
        }
        return user
      } catch (err) {
        return null
      } finally {
        setIsLookingUp(false)
      }
    },
    enabled: !!guildId && newUserId.trim().length >= 17 && /^\d+$/.test(newUserId.trim()),
    retry: false,
    staleTime: 60000,
  })

  // ── Fetch blacklist ────────────────────────────────────────────────────────
  const { data: blacklist, isLoading, isError } = useQuery({
    queryKey: ['blacklist', guildId],
    queryFn: () => blacklistApi.getBlacklist(guildId!, 100),
    enabled: !!guildId,
    refetchInterval: 5000,
  })

  // ── Add mutation ───────────────────────────────────────────────────────────
  const { mutate: addUser, isPending: isAdding } = useMutation({
    mutationFn: () =>
      blacklistApi.addToBlacklist(guildId!, {
        targetUserId: newUserId.trim(),
        targetUsername: newUserName.trim() || `Discord User #${newUserId.slice(-4)}`,
        targetAvatarUrl: targetAvatarUrl,
        reason: newUserReason.trim() || (lang === 'vi' ? 'Vi phạm quy định' : 'Rule violation'),
        notes: newUserNotes.trim() || null,
      }),
    onSuccess: () => {
      toast.success(lang === 'vi' ? 'Đã thêm vào danh sách cấm!' : 'User added to blacklist!')
      qc.invalidateQueries({ queryKey: ['blacklist', guildId] })
      setNewUserId('')
      setNewUserName('')
      setTargetAvatarUrl(null)
      setNewUserReason('')
      setNewUserNotes('')
    },
    onError: (err: any) => {
      const msg = err?.response?.data?.error ?? (lang === 'vi' ? 'Thêm thất bại.' : 'Failed to add user.')
      toast.error(msg)
    },
  })

  // ── Remove mutation ────────────────────────────────────────────────────────
  const { mutate: removeUser, isPending: isRemoving } = useMutation({
    mutationFn: ({ userId, reason }: { userId: string; reason: string }) =>
      blacklistApi.removeFromBlacklist(guildId!, userId, reason),
    onSuccess: () => {
      toast.success(lang === 'vi' ? 'Đã xóa khỏi danh sách cấm!' : 'User removed from blacklist!')
      qc.invalidateQueries({ queryKey: ['blacklist', guildId] })
    },
    onError: () => toast.error(lang === 'vi' ? 'Xóa thất bại.' : 'Failed to remove user.'),
  })

  const handleAddUser = () => {
    if (!newUserId.trim()) return
    if (!newUserReason.trim()) {
      toast.error(lang === 'vi' ? 'Vui lòng nhập lý do cấm.' : 'Please provide a ban reason.')
      return
    }
    addUser()
  }

  const handleRemoveUser = async (entry: BlacklistEntry) => {
    const ok = await confirm({
      title: lang === 'vi' ? 'Xóa khỏi Blacklist' : 'Remove from Blacklist',
      message: lang === 'vi'
        ? `Bạn có chắc muốn xóa ${entry.username} khỏi danh sách cấm? Bot sẽ không còn tự động ban họ khi rejoin.`
        : `Are you sure you want to remove ${entry.username} from the blacklist? The bot will no longer auto-ban them on rejoin.`,
      confirmLabel: lang === 'vi' ? 'Xóa ngay' : 'Remove',
      cancelLabel: lang === 'vi' ? 'Hủy' : 'Cancel',
      variant: 'danger',
    })
    if (!ok) return

    const reason = removeReason.trim() || (lang === 'vi' ? 'Xóa qua dashboard' : 'Removed via dashboard')
    removeUser({ userId: entry.userId, reason })
  }

  // Filter
  const filtered = (blacklist ?? []).filter(
    (e) =>
      e.username.toLowerCase().includes(search.toLowerCase()) ||
      e.userId.includes(search) ||
      e.reason.toLowerCase().includes(search.toLowerCase()),
  )

  return (
    <div className="max-w-4xl space-y-8">
      <div>
        <h2 className="text-xl font-bold text-[--color-text] flex items-center gap-2.5">
          <Ban className="text-[--color-danger]" />
          {lang === 'vi' ? 'Quản Lý Danh Sách Cấm (User Blacklist)' : 'User Blacklist Management'}
        </h2>
        <p className="text-sm text-[--color-text-muted] mt-1">
          {lang === 'vi'
            ? 'Người dùng trong danh sách này sẽ bị Bot tự động ban lại ngay lập tức nếu họ cố rejoin vào máy chủ.'
            : 'Users in this list will be automatically re-banned by the bot if they attempt to rejoin the server.'}
        </p>
      </div>

      {/* Add User Card */}
      <Card className="space-y-4 border-l-4 border-l-[--color-danger] bg-[--color-surface]">
        <div className="flex items-center gap-2">
          <UserX className="text-[--color-danger]" size={20} />
          <div>
            <h3 className="font-semibold text-base text-[--color-text]">
              {lang === 'vi' ? 'Thêm Người Dùng Vào Danh Sách Cấm' : 'Add User to Blacklist'}
            </h3>
            <p className="text-xs text-[--color-text-muted]">
              {lang === 'vi'
                ? 'Nhập Discord User ID để cấm vĩnh viễn — Bot sẽ tự động ban ngay khi họ rejoin'
                : 'Enter Discord User ID to permanently blacklist — bot auto-bans on rejoin'}
            </p>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-12 gap-2">
          <input
            type="text"
            placeholder="Discord User ID (vd: 1514609829610782862)"
            value={newUserId}
            onChange={(e) => {
              setNewUserId(e.target.value)
              if (e.target.value.trim().length < 17) {
                setTargetAvatarUrl(null)
              }
            }}
            className="sm:col-span-4 px-3 py-2 rounded-md bg-[--color-surface-alt] border border-[--color-border] text-sm text-[--color-text] focus:ring-2 focus:ring-[--color-danger] outline-none"
          />
          <div className="sm:col-span-3 relative flex items-center">
            {targetAvatarUrl && (
              <img src={targetAvatarUrl} alt="avatar" className="w-5 h-5 rounded-full absolute left-2.5" />
            )}
            <input
              type="text"
              placeholder={isLookingUp ? (lang === 'vi' ? 'Đang tìm tên...' : 'Looking up...') : (lang === 'vi' ? 'Tên hiển thị (tự động điền)' : 'Display Name (auto-filled)')}
              value={newUserName}
              onChange={(e) => setNewUserName(e.target.value)}
              className={`w-full ${targetAvatarUrl ? 'pl-9 pr-8' : 'px-3'} py-2 rounded-md bg-[--color-surface-alt] border border-[--color-border] text-sm text-[--color-text] focus:ring-2 focus:ring-[--color-brand] outline-none`}
            />
            {isLookingUp && (
              <div className="absolute right-2.5">
                <Spinner size="sm" />
              </div>
            )}
          </div>
          <input
            type="text"
            placeholder={lang === 'vi' ? 'Lý do cấm (bắt buộc)' : 'Ban reason (required)'}
            value={newUserReason}
            onChange={(e) => setNewUserReason(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleAddUser()}
            className="sm:col-span-3 px-3 py-2 rounded-md bg-[--color-surface-alt] border border-[--color-border] text-sm text-[--color-text] focus:ring-2 focus:ring-[--color-brand] outline-none"
          />
          <Button
            onClick={handleAddUser}
            variant="danger"
            loading={isAdding}
            className="sm:col-span-2 gap-1 cursor-pointer justify-center"
          >
            <Ban size={15} /> {lang === 'vi' ? 'Cấm User' : 'Blacklist'}
          </Button>
        </div>

        {/* Optional notes field */}
        <input
          type="text"
          placeholder={lang === 'vi' ? 'Ghi chú thêm (tài khoản phụ, ngữ cảnh, ...) — tùy chọn' : 'Extra notes (alt accounts, context, ...) — optional'}
          value={newUserNotes}
          onChange={(e) => setNewUserNotes(e.target.value)}
          className="w-full px-3 py-2 rounded-md bg-[--color-surface-alt] border border-[--color-border] text-sm text-[--color-text] focus:ring-2 focus:ring-[--color-brand] outline-none"
        />
      </Card>

      {/* Blacklisted Users Table */}
      <Card className="space-y-4 bg-[--color-surface] p-0 overflow-hidden">
        <div className="p-5 pb-0 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <ShieldOff className="text-[--color-danger]" size={18} />
            <h3 className="font-semibold text-base text-[--color-text]">
              {lang === 'vi' ? 'Danh Sách Cấm Hiện Tại' : 'Current Blacklist'}
            </h3>
            <Badge variant="danger" className="text-xs">
              {(blacklist ?? []).length} {lang === 'vi' ? 'người' : 'users'}
            </Badge>
          </div>

          {/* Search */}
          <div className="relative w-full sm:w-60">
            <Search size={14} className="absolute left-3 top-2.5 text-[--color-text-muted]" />
            <input
              type="text"
              placeholder={lang === 'vi' ? 'Tìm ID, tên, lý do...' : 'Search ID, name, reason...'}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-8 pr-3 py-2 rounded-md bg-[--color-surface-alt] border border-[--color-border] text-xs text-[--color-text] focus:ring-2 focus:ring-[--color-brand] outline-none"
            />
          </div>
        </div>

        {isLoading ? (
          <div className="py-16 flex justify-center">
            <Spinner size="lg" />
          </div>
        ) : isError ? (
          <div className="py-12 text-center text-[--color-danger] text-sm font-semibold">
            {lang === 'vi' ? 'Không thể tải danh sách cấm.' : 'Failed to load blacklist.'}
          </div>
        ) : filtered.length === 0 ? (
          <div className="py-16 text-center text-[--color-text-muted] text-sm">
            {search
              ? (lang === 'vi' ? 'Không tìm thấy kết quả.' : 'No results found.')
              : (lang === 'vi' ? 'Danh sách cấm đang trống.' : 'Blacklist is empty.')}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[--color-border] text-[--color-text-muted] text-xs uppercase">
                <th className="px-5 py-3 text-left">{lang === 'vi' ? 'Người Dùng' : 'User'}</th>
                <th className="px-5 py-3 text-left">{lang === 'vi' ? 'Lý Do' : 'Reason'}</th>
                <th className="px-5 py-3 text-left">{lang === 'vi' ? 'Ngày Cấm' : 'Banned On'}</th>
                <th className="px-5 py-3 text-right">{lang === 'vi' ? 'Hành Động' : 'Actions'}</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((entry) => (
                <tr
                  key={entry.userId}
                  className="border-b border-[--color-border] last:border-0 hover:bg-[--color-surface-raised] transition-colors"
                >
                  <td className="px-5 py-3">
                    <div className="flex items-center gap-3">
                      {entry.avatarUrl ? (
                        <img
                          src={entry.avatarUrl}
                          alt={entry.username}
                          className="w-8 h-8 rounded-full border border-[--color-border] object-cover flex-shrink-0"
                        />
                      ) : (
                        <div className="w-8 h-8 rounded-full bg-[--color-surface-raised] border border-[--color-border] flex items-center justify-center flex-shrink-0">
                          <span className="text-xs font-bold text-[--color-text-muted]">
                            {entry.username.charAt(0).toUpperCase()}
                          </span>
                        </div>
                      )}
                      <div>
                        <div className="flex items-center gap-1.5">
                          <Badge variant="danger" className="text-xs font-bold py-0.5 px-2">
                            {entry.username}
                          </Badge>
                        </div>
                        <span className="font-mono text-xs text-pink-400 mt-0.5 block">
                          ID: {entry.userId}
                        </span>
                      </div>
                    </div>
                  </td>
                  <td className="px-5 py-3">
                    <p className="text-[--color-text] text-xs font-medium">{entry.reason}</p>
                    {entry.notes && (
                      <p className="text-[--color-text-muted] text-xs mt-0.5 italic">{entry.notes}</p>
                    )}
                  </td>
                  <td className="px-5 py-3 text-[--color-text-muted] text-xs whitespace-nowrap">
                    <span className="flex items-center gap-1">
                      <Clock size={12} />
                      {formatDate(entry.createdAt)}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-right">
                    <Button
                      variant="ghost"
                      size="sm"
                      disabled={isRemoving}
                      onClick={() => handleRemoveUser(entry)}
                      className="text-[--color-danger] hover:text-[--color-danger] cursor-pointer"
                      title={lang === 'vi' ? 'Xóa khỏi blacklist' : 'Remove from blacklist'}
                    >
                      <Trash2 size={14} />
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>

      {/* Remove Reason field (used for all removals) */}
      <Card className="p-4 bg-[--color-surface-alt] border-[--color-border]">
        <label className="block text-xs font-medium text-[--color-text-muted] mb-1.5 uppercase tracking-wide">
          {lang === 'vi' ? 'Lý Do Xóa (cho thao tác xóa tiếp theo)' : 'Removal Reason (for next removal)'}
        </label>
        <input
          type="text"
          placeholder={lang === 'vi' ? 'Gõ lý do xóa trước khi nhấn nút xóa...' : 'Enter removal reason before clicking remove...'}
          value={removeReason}
          onChange={(e) => setRemoveReason(e.target.value)}
          className="w-full px-3 py-2 rounded-md bg-[--color-surface] border border-[--color-border] text-sm text-[--color-text] focus:ring-2 focus:ring-[--color-brand] outline-none"
        />
      </Card>
    </div>
  )
}
