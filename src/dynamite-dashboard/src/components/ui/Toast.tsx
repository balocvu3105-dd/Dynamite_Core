import { useEffect, useState } from 'react'
import { CheckCircle2, XCircle, Info, AlertTriangle, X } from 'lucide-react'
import { useToastStore, type Toast, type ToastType } from '@/store/toastStore'

// ─── Single Toast Item ────────────────────────────────────────────────────────

const ICONS: Record<ToastType, React.ReactNode> = {
    success: <CheckCircle2 size={16} className="text-emerald-400 shrink-0" />,
    error: <XCircle size={16} className="text-red-400 shrink-0" />,
    info: <Info size={16} className="text-blue-400 shrink-0" />,
    warning: <AlertTriangle size={16} className="text-yellow-400 shrink-0" />,
}

const BAR_COLOR: Record<ToastType, string> = {
    success: 'bg-emerald-500',
    error: 'bg-red-500',
    info: 'bg-blue-500',
    warning: 'bg-yellow-500',
}

function ToastItem({ toast }: { toast: Toast }) {
    const remove = useToastStore((s) => s.remove)
    const [visible, setVisible] = useState(false)

    // Trigger enter animation after mount
    useEffect(() => {
        const t = requestAnimationFrame(() => setVisible(true))
        return () => cancelAnimationFrame(t)
    }, [])

    return (
        <div
            className={`
        relative flex items-start gap-3 px-4 py-3 rounded-lg shadow-lg
        bg-[--color-surface-alt] border border-[--color-border]
        min-w-[280px] max-w-[380px] overflow-hidden
        transition-all duration-300 ease-out
        ${visible ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-2'}
      `}
            role="alert"
        >
            {/* Accent bar */}
            <div className={`absolute left-0 top-0 bottom-0 w-1 ${BAR_COLOR[toast.type]} rounded-l-lg`} />

            {/* Icon */}
            <span className="mt-0.5">{ICONS[toast.type]}</span>

            {/* Message */}
            <p className="flex-1 text-sm text-[--color-text] leading-snug pr-2">{toast.message}</p>

            {/* Close button */}
            <button
                onClick={() => remove(toast.id)}
                className="mt-0.5 text-[--color-text-muted] hover:text-[--color-text] transition-colors"
                aria-label="Dismiss"
            >
                <X size={14} />
            </button>
        </div>
    )
}

// ─── Toast Container (rendered once in UIProvider) ────────────────────────────

export function ToastContainer() {
    const toasts = useToastStore((s) => s.toasts)

    return (
        <div
            className="fixed bottom-6 right-6 z-50 flex flex-col gap-2 pointer-events-none"
            aria-live="polite"
            aria-atomic="false"
        >
            {toasts.map((t) => (
                <div key={t.id} className="pointer-events-auto">
                    <ToastItem toast={t} />
                </div>
            ))}
        </div>
    )
}