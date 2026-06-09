import { AlertTriangle } from 'lucide-react'
import { useConfirmStore } from '@/store/confirmStore'
import { Button } from '@/components/ui'

/**
 * Rendered once in UIProvider.
 * Controlled entirely by confirmStore — no local state needed.
 */
export function ConfirmDialog() {
    const { isOpen, options, confirm, cancel } = useConfirmStore()

    if (!isOpen || !options) return null

    const isDanger = options.variant === 'danger'

    return (
        // Backdrop
        <div
            className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm"
            onClick={cancel}
            role="dialog"
            aria-modal="true"
            aria-labelledby="confirm-title"
        >
            {/* Dialog panel */}
            <div
                className="bg-[--color-surface-alt] border border-[--color-border] rounded-xl shadow-2xl w-full max-w-sm mx-4 p-6 animate-in"
                onClick={(e) => e.stopPropagation()} // prevent backdrop click from closing
            >
                {/* Icon + Title */}
                <div className="flex items-start gap-4 mb-4">
                    {isDanger && (
                        <div className="w-10 h-10 rounded-full bg-red-500/10 flex items-center justify-center shrink-0">
                            <AlertTriangle size={18} className="text-red-400" />
                        </div>
                    )}
                    <div>
                        {options.title && (
                            <h3 id="confirm-title" className="font-semibold text-[--color-text] mb-1">
                                {options.title}
                            </h3>
                        )}
                        <p className="text-sm text-[--color-text-muted] leading-relaxed">
                            {options.message}
                        </p>
                    </div>
                </div>

                {/* Actions */}
                <div className="flex justify-end gap-2 mt-6">
                    <Button variant="secondary" size="sm" onClick={cancel}>
                        {options.cancelLabel ?? 'Cancel'}
                    </Button>
                    <Button
                        variant={isDanger ? 'danger' : 'primary'}
                        size="sm"
                        onClick={confirm}
                    >
                        {options.confirmLabel ?? 'Confirm'}
                    </Button>
                </div>
            </div>
        </div>
    )
}