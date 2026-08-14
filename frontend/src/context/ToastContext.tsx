import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from 'react'

type ToastVariant = 'success' | 'error'

interface ToastAction {
  label: string
  onClick: () => void
}

interface Toast {
  id: string
  message: string
  variant: ToastVariant
  action?: ToastAction
}

interface ToastContextValue {
  showToast: (message: string, variant?: ToastVariant, action?: ToastAction) => void
}

const ToastContext = createContext<ToastContextValue | null>(null)

// A toast with an action (currently just "Undo") stays up longer than a
// plain confirmation — 3.5s is barely enough to read "Archived", let alone
// notice and click a button too.
const PLAIN_DURATION_MS = 3500
const ACTION_DURATION_MS = 6000

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const idCounter = useRef(0)

  const showToast = useCallback((message: string, variant: ToastVariant = 'success', action?: ToastAction) => {
    const id = String(++idCounter.current)
    setToasts((prev) => [...prev, { id, message, variant, action }])
    setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== id))
    }, action ? ACTION_DURATION_MS : PLAIN_DURATION_MS)
  }, [])

  const dismiss = (id: string) => setToasts((prev) => prev.filter((t) => t.id !== id))

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div className="toast-stack" role="status" aria-live="polite">
        {toasts.map((toast) => (
          <div key={toast.id} className={`toast toast-${toast.variant}`}>
            <span>{toast.message}</span>
            {toast.action && (
              <button
                className="toast-action"
                onClick={() => {
                  toast.action!.onClick()
                  dismiss(toast.id)
                }}
              >
                {toast.action.label}
              </button>
            )}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

export function useToast() {
  const context = useContext(ToastContext)
  if (!context) throw new Error('useToast must be used within a ToastProvider')
  return context
}
