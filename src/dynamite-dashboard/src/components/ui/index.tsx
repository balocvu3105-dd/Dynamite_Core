import { cn } from '@/lib/utils'
import type { ButtonHTMLAttributes, HTMLAttributes, ReactNode } from 'react'

// ── Button ────────────────────────────────────────────────────────────────────
interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost'
  size?: 'sm' | 'md' | 'lg'
  loading?: boolean
}

export function Button({
  variant = 'primary',
  size = 'md',
  loading,
  className,
  children,
  disabled,
  ...props
}: ButtonProps) {
  const variantClass = {
    primary: 'bg-(--color-brand) text-white hover:bg-(--color-brand-hover) focus:ring-(--color-brand)',
    secondary: 'bg-(--color-surface-alt) text-(--color-text) hover:bg-(--color-border) focus:ring-(--color-border)',
    danger: 'bg-(--color-danger) text-white hover:opacity-90 focus:ring-(--color-danger)',
    ghost: 'text-(--color-text-muted) hover:text-(--color-text) hover:bg-(--color-surface-alt)',
  }[variant]

  const sizeClass = {
    sm: 'px-3 py-1.5 text-sm',
    md: 'px-4 py-2 text-sm',
    lg: 'px-6 py-3 text-base',
  }[size]

  return (
    <button
      disabled={disabled || loading}
      className={cn(
        'inline-flex items-center justify-center gap-2 rounded-md font-medium transition-colors',
        'focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-(--color-surface)',
        'disabled:opacity-50 disabled:cursor-not-allowed',
        variantClass,
        sizeClass,
        className
      )}
      {...props}
    >
      {loading && <Spinner size="sm" />}
      {children}
    </button>
  )
}

// ── Spinner ───────────────────────────────────────────────────────────────────
interface SpinnerProps {
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

export function Spinner({ size = 'md', className }: SpinnerProps) {
  const sizeClass = { sm: 'w-4 h-4', md: 'w-6 h-6', lg: 'w-8 h-8' }[size]
  return (
    <div
      className={cn(
        'animate-spin rounded-full border-2 border-(--color-border) border-t-(--color-brand)',
        sizeClass,
        className
      )}
    />
  )
}

// ── Card ──────────────────────────────────────────────────────────────────────
interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
}

export function Card({ className, children, ...props }: CardProps) {
  return (
    <div
      className={cn(
        'bg-(--color-surface-alt) border border-(--color-border) rounded-lg p-5',
        className
      )}
      {...props}
    >
      {children}
    </div>
  )
}

// ── Badge ─────────────────────────────────────────────────────────────────────
interface BadgeProps {
  variant?: 'success' | 'danger' | 'warning' | 'default'
  children: ReactNode
  className?: string
}

export function Badge({ variant = 'default', children, className }: BadgeProps) {
  const variantClass = {
    success: 'bg-(--color-success)/15 text-(--color-success)',
    danger: 'bg-(--color-danger)/15 text-(--color-danger)',
    warning: 'bg-(--color-warning)/15 text-(--color-warning)',
    default: 'bg-(--color-border) text-(--color-text-muted)',
  }[variant]

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium',
        variantClass,
        className
      )}
    >
      {children}
    </span>
  )
}

// ── Toggle ────────────────────────────────────────────────────────────────────
interface ToggleProps {
  checked: boolean
  onChange: (checked: boolean) => void
  disabled?: boolean
}

export function Toggle({ checked, onChange, disabled }: ToggleProps) {
  return (
    <button
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      style={{
        backgroundColor: checked ? 'var(--color-brand)' : 'var(--color-border)',
      }}
      className={cn(
        'relative inline-flex h-6 w-11 flex-shrink-0 items-center rounded-full transition-colors',
        'focus:outline-none focus:ring-2 focus:ring-(--color-brand) focus:ring-offset-2 focus:ring-offset-(--color-surface)',
        'disabled:opacity-50 disabled:cursor-not-allowed'
      )}
    >
      <span
        className={cn(
          'inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform',
          checked ? 'translate-x-6' : 'translate-x-1'
        )}
      />
    </button>
  )
}

// ── Input ─────────────────────────────────────────────────────────────────────
interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

export function Input({ label, error, className, id, ...props }: InputProps) {
  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={id} className="text-sm font-medium text-(--color-text)">
          {label}
        </label>
      )}
      <input
        id={id}
        className={cn(
          'w-full rounded-md border border-(--color-border) bg-(--color-surface)',
          'px-3 py-2 text-sm text-(--color-text) placeholder:text-(--color-text-muted)',
          'focus:outline-none focus:ring-2 focus:ring-(--color-brand) focus:border-transparent transition-colors',
          error && 'border-(--color-danger)',
          className
        )}
        {...props}
      />
      {error && <p className="text-xs text-(--color-danger)">{error}</p>}
    </div>
  )
}

// ── Select ────────────────────────────────────────────────────────────────────
interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label?: string
  placeholder?: string
}

export function Select({ label, placeholder, className, id, children, ...props }: SelectProps) {
  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={id} className="text-sm font-medium text-(--color-text)">
          {label}
        </label>
      )}
      <select
        id={id}
        className={cn(
          'w-full rounded-md border border-(--color-border) bg-(--color-surface)',
          'px-3 py-2 text-sm text-(--color-text)',
          'focus:outline-none focus:ring-2 focus:ring-(--color-brand) focus:border-transparent transition-colors',
          className
        )}
        {...props}
      >
        {placeholder && <option value="" disabled>{placeholder}</option>}
        {children}
      </select>
    </div>
  )
}