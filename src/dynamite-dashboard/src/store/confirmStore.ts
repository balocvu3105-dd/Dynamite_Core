import { create } from 'zustand'

export interface ConfirmOptions {
    title?: string
    message: string
    confirmLabel?: string
    cancelLabel?: string
    variant?: 'danger' | 'default'
}

interface ConfirmState {
    isOpen: boolean
    options: ConfirmOptions | null
    _resolve: ((value: boolean) => void) | null

    open: (options: ConfirmOptions) => Promise<boolean>
    confirm: () => void
    cancel: () => void
}

export const useConfirmStore = create<ConfirmState>((set, get) => ({
    isOpen: false,
    options: null,
    _resolve: null,

    open: (options) => {
        return new Promise<boolean>((resolve) => {
            set({ isOpen: true, options, _resolve: resolve })
        })
    },

    confirm: () => {
        get()._resolve?.(true)
        set({ isOpen: false, options: null, _resolve: null })
    },

    cancel: () => {
        get()._resolve?.(false)
        set({ isOpen: false, options: null, _resolve: null })
    },
}))