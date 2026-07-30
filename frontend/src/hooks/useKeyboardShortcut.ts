import { useEffect } from 'react'

/**
 * Fires `handler` when `key` is pressed, unless focus is inside a text
 * input/textarea/select or a modifier key is held — otherwise "c" for
 * "create story" would fire while someone is typing a "c" into a title field.
 */
export function useKeyboardShortcut(key: string, handler: () => void, enabled = true) {
  useEffect(() => {
    if (!enabled) return

    const listener = (e: KeyboardEvent) => {
      if (e.key.toLowerCase() !== key.toLowerCase()) return
      if (e.metaKey || e.ctrlKey || e.altKey) return

      const target = e.target as HTMLElement | null
      const tag = target?.tagName
      const isTyping = tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || target?.isContentEditable
      if (isTyping) return

      e.preventDefault()
      handler()
    }

    window.addEventListener('keydown', listener)
    return () => window.removeEventListener('keydown', listener)
  }, [key, handler, enabled])
}
