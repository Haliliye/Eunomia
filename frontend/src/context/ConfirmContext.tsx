import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from 'react'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface ConfirmOptions {
  title: string
  description?: string
  confirmLabel?: string
  danger?: boolean
}

interface PendingConfirm extends ConfirmOptions {
  resolve: (value: boolean) => void
}

interface ConfirmContextValue {
  confirm: (options: ConfirmOptions | string) => Promise<boolean>
}

const ConfirmContext = createContext<ConfirmContextValue | null>(null)

// Replaces window.confirm — same call-and-await shape (`if (!await confirm(...)) return`),
// but rendered in-app so it matches the theme/dark-mode and works consistently
// on mobile, instead of the browser's native (and inconsistently styled) dialog.
export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [pending, setPending] = useState<PendingConfirm | null>(null)
  const resolverRef = useRef<((value: boolean) => void) | null>(null)

  const confirm = useCallback((options: ConfirmOptions | string) => {
    const normalized = typeof options === 'string' ? { title: options } : options
    return new Promise<boolean>((resolve) => {
      resolverRef.current = resolve
      setPending({ ...normalized, resolve })
    })
  }, [])

  const settle = (value: boolean) => {
    resolverRef.current?.(value)
    resolverRef.current = null
    setPending(null)
  }

  useEscapeToClose(pending !== null, () => settle(false))
  const containerRef = useFocusTrap(pending !== null)

  return (
    <ConfirmContext.Provider value={{ confirm }}>
      {children}
      {pending && (
        <div className="modal-overlay" onClick={() => settle(false)}>
          <div ref={containerRef} className="modal" role="alertdialog" aria-modal="true" aria-labelledby="confirm-dialog-title" style={{ maxWidth: 420 }} onClick={(e) => e.stopPropagation()}>
            <h2 id="confirm-dialog-title">{pending.title}</h2>
            {pending.description && <p style={{ fontSize: 13, marginTop: 4 }}>{pending.description}</p>}
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" autoFocus onClick={() => settle(false)}>Cancel</button>
              <button
                className={pending.danger ? 'btn btn-danger' : 'btn btn-primary'}
                onClick={() => settle(true)}
              >
                {pending.confirmLabel ?? 'Confirm'}
              </button>
            </div>
          </div>
        </div>
      )}
    </ConfirmContext.Provider>
  )
}

export function useConfirm() {
  const context = useContext(ConfirmContext)
  if (!context) throw new Error('useConfirm must be used within a ConfirmProvider')
  return context.confirm
}
