import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { Header } from './Header'

const PAGE_TITLES: Record<string, string> = {
    overview: 'Overview',
    moderation: 'Moderation',
    logging: 'Logging',
    welcome: 'Welcome & Verify',
    security: 'Security',
}

export function DashboardLayout() {
    // Read last segment of path for title
    const parts = window.location.pathname.split('/')
    const segment = parts[parts.length - 1] ?? ''
    const title = PAGE_TITLES[segment] ?? 'Dashboard'

    return (
        <div className="flex min-h-screen bg-[--color-surface]">
            <Sidebar />
            <div className="flex-1 flex flex-col min-w-0">
                <Header title={title} />
                <main className="flex-1 p-6 overflow-auto">
                    <Outlet />
                </main>
            </div>
        </div>
    )
}