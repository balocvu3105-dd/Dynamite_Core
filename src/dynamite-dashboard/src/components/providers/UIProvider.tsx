import { ToastContainer } from '@/components/ui/Toast'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'

interface UIProviderProps {
    children: React.ReactNode
}

/**
 * Mounts all global UI primitives (toasts, dialogs) once at the root.
 * Keeps main.tsx clean and makes it easy to add future globals (e.g. CommandPalette).
 */
export function UIProvider({ children }: UIProviderProps) {
    return (
        <>
            {children}
            <ToastContainer />
            <ConfirmDialog />
        </>
    )
}