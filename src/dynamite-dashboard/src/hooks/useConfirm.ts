import { useConfirmStore } from '@/store/confirmStore'
import type { ConfirmOptions } from '@/store/confirmStore'

export type { ConfirmOptions }

export function useConfirm() {
    return useConfirmStore((s) => s.open)
}