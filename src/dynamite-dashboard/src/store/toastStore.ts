import { create } from 'zustand'

export type ToastType = 'success' | 'error' | 'info' | 'warning'

export interface Toast {
    id: string
    message: string
    type: ToastType
    duration?: number
}

interface ToastStore {
    toasts: Toast[]
    add: (toast: Omit<Toast, 'id'>) => void
    remove: (id: string) => void
}

export const useToastStore = create<ToastStore>((set) => ({
    toasts: [],

    add: (toast) => {
        const id = crypto.randomUUID()
        set((state) => ({
            toasts: [...state.toasts, { ...toast, id }],
        }))
        // Auto-remove after duration (default 3.5s)
        setTimeout(() => {
            set((state) => ({
                toasts: state.toasts.filter((t) => t.id !== id),
            }))
        }, toast.duration ?? 3500)
    },

    remove: (id) =>
        set((state) => ({
            toasts: state.toasts.filter((t) => t.id !== id),
        })),
}))