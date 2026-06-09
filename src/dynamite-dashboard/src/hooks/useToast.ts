import { useToastStore } from '@/store/toastStore'

/**
 * Convenience hook for triggering toasts anywhere in the app.
 *
 * Usage:
 *   const toast = useToast()
 *   toast.success('Settings saved!')
 *   toast.error('Something went wrong.')
 */
export function useToast() {
    const add = useToastStore((s) => s.add)

    return {
        success: (message: string, duration?: number) =>
            add({ type: 'success', message, duration }),

        error: (message: string, duration?: number) =>
            add({ type: 'error', message, duration: duration ?? 5000 }),

        info: (message: string, duration?: number) =>
            add({ type: 'info', message, duration }),

        warning: (message: string, duration?: number) =>
            add({ type: 'warning', message, duration }),
    }
}