import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Trophy, Coins, Search, Edit3, Save } from 'lucide-react'
import { economyApi, type LeaderboardUser } from '@/api/economy'
import { Card, Button, Spinner, Badge } from '@/components/ui'
import { useToast } from '@/hooks/useToast'
import { useLangStore } from '@/i18n'

export default function EconomyPage() {
  const { guildId } = useParams<{ guildId: string }>()
  const qc = useQueryClient()
  const toast = useToast()
  const { t, lang } = useLangStore()

  const [searchUserId, setSearchUserId] = useState('')
  const [activeUserId, setActiveUserId] = useState<string | null>(null)
  const [editCoins, setEditCoins] = useState<number>(0)

  const { data: leaderboard, isLoading: loadingBoard } = useQuery({
    queryKey: ['economy-leaderboard', guildId],
    queryFn: () => economyApi.getLeaderboard(guildId!),
    enabled: !!guildId,
  })

  const {
    data: userWallet,
    isLoading: loadingUser,
    refetch: refetchUser,
  } = useQuery({
    queryKey: ['user-wallet', guildId, activeUserId],
    queryFn: () => economyApi.getUserWallet(guildId!, activeUserId!),
    enabled: !!guildId && !!activeUserId,
  })

  const { mutate: saveBalance, isPending: isSaving } = useMutation({
    mutationFn: () => economyApi.updateBalance(guildId!, activeUserId!, editCoins),
    onSuccess: () => {
      toast.success('Balance adjusted successfully.')
      qc.invalidateQueries({ queryKey: ['economy-leaderboard', guildId] })
      refetchUser()
    },
    onError: () => toast.error('Failed to adjust user balance.'),
  })

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    if (!searchUserId.trim()) return
    setActiveUserId(searchUserId.trim())
  }

  return (
    <div className="max-w-4xl space-y-8">
      <div>
        <h2 className="text-xl font-bold text-[--color-text] flex items-center gap-2">
          <Coins className="text-amber-400" /> {t.economy.title}
        </h2>
        <p className="text-sm text-[--color-text-muted] mt-1">
          {t.economy.subtitle}
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* User Balance Lookup */}
        <Card className="md:col-span-1 space-y-4">
          <h3 className="text-md font-semibold text-[--color-text] flex items-center gap-2">
            <Search size={16} /> {lang === 'vi' ? 'Kiểm tra & Chỉnh sửa ví' : 'Inspect User Wallet'}
          </h3>

          <form onSubmit={handleSearch} className="flex gap-2">
            <input
              type="text"
              placeholder={t.economy.searchUser}
              value={searchUserId}
              onChange={(e) => setSearchUserId(e.target.value)}
              className="flex-1 bg-[--color-surface-alt] border border-[--color-border] rounded px-3 py-1.5 text-xs text-[--color-text]"
            />
            <Button type="submit" size="sm">
              {t.economy.searchBtn}
            </Button>
          </form>

          {loadingUser ? (
            <div className="py-8 flex justify-center">
              <Spinner size="sm" />
            </div>
          ) : userWallet ? (
            <div className="pt-3 border-t border-[--color-border] space-y-3">
              <div className="flex justify-between text-xs">
                <span className="text-[--color-text-muted]">ID:</span>
                <span className="font-mono text-[--color-text]">{userWallet.userId}</span>
              </div>
              <div className="flex justify-between text-xs">
                <span className="text-[--color-text-muted]">{lang === 'vi' ? 'Chuỗi điểm danh:' : 'Daily Streak:'}</span>
                <Badge variant="warning">{userWallet.dailyStreak} {lang === 'vi' ? 'ngày' : 'days'} 🔥</Badge>
              </div>

              <div className="pt-2">
                <label className="block text-xs text-[--color-text-muted] mb-1">{t.economy.currentBalance}</label>
                <div className="flex gap-2">
                  <input
                    type="number"
                    defaultValue={userWallet.coins}
                    onChange={(e) => setEditCoins(Number(e.target.value))}
                    className="flex-1 bg-[--color-surface] border border-[--color-border] rounded px-2.5 py-1 text-xs font-semibold text-amber-400"
                  />
                  <Button
                    size="sm"
                    onClick={() => saveBalance()}
                    loading={isSaving}
                    className="flex items-center gap-1"
                  >
                    <Save size={14} /> {t.common.save}
                  </Button>
                </div>
              </div>
            </div>
          ) : activeUserId ? (
            <p className="text-xs text-red-400 pt-2">{lang === 'vi' ? `Không tìm thấy ví cho ID ${activeUserId}.` : `No wallet found for ID ${activeUserId}.`}</p>
          ) : null}
        </Card>

        {/* Server Leaderboard */}
        <Card className="md:col-span-2 space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-md font-semibold text-[--color-text] flex items-center gap-2">
              <Trophy size={16} className="text-amber-400" /> {t.economy.leaderboard}
            </h3>
            <Badge>Realtime DB Query</Badge>
          </div>

          {loadingBoard ? (
            <div className="py-12 flex justify-center">
              <Spinner />
            </div>
          ) : !leaderboard || leaderboard.length === 0 ? (
            <p className="text-xs text-[--color-text-muted] py-6 text-center">{t.economy.noUsers}</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead>
                  <tr className="border-b border-[--color-border] text-[--color-text-muted]">
                    <th className="pb-2.5 font-semibold">{t.economy.rank}</th>
                    <th className="pb-2.5 font-semibold">User ID</th>
                    <th className="pb-2.5 font-semibold text-right">{t.economy.balance}</th>
                    <th className="pb-2.5 font-semibold text-right">{t.economy.actions}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[--color-border]/50">
                  {leaderboard.map((u: LeaderboardUser) => (
                    <tr key={u.userId} className="hover:bg-[--color-surface-alt]/50 transition-colors">
                      <td className="py-2.5 font-bold">
                        {u.rank === 1 ? '🥇' : u.rank === 2 ? '🥈' : u.rank === 3 ? '🥉' : `#${u.rank}`}
                      </td>
                      <td className="py-2.5 font-mono text-[--color-text]">{u.userId}</td>
                      <td className="py-2.5 text-right font-semibold text-amber-400">
                        {u.coins.toLocaleString()} 💰
                      </td>
                      <td className="py-2.5 text-right">
                        <button
                          onClick={() => {
                            setActiveUserId(u.userId)
                            setEditCoins(u.coins)
                          }}
                          className="text-xs text-[--color-brand] hover:underline flex items-center gap-1 ml-auto cursor-pointer"
                        >
                          <Edit3 size={12} /> {t.economy.inspectWallet}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      </div>
    </div>
  )
}
