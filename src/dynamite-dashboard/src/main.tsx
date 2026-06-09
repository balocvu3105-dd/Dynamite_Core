import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { UIProvider } from '@/components/providers/UIProvider'
import App from './App'
import './index.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <UIProvider>
      <App />
    </UIProvider>
  </StrictMode>,
)