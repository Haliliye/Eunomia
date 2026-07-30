import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { accountApi, type GlobalSearchResult } from '@/api/auth'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface CommandPaletteProps {
  isOpen: boolean
  onOpenChange: (isOpen: boolean) => void
}

// Ctrl/Cmd+K command palette — searches across every team the person is a
// member of (see GlobalSearchQueryHandler), since the existing search box on
// each team's Backlog only ever searched within that one team.
export default function CommandPalette({ isOpen, onOpenChange }: CommandPaletteProps) {
  const navigate = useNavigate()
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<GlobalSearchResult[]>([])
  const [isSearching, setSearching] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)
  const containerRef = useFocusTrap(isOpen)

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        onOpenChange(!isOpen)
      }
      if (e.key === 'Escape') onOpenChange(false)
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, onOpenChange])

  useEffect(() => {
    if (isOpen) {
      setQuery('')
      setResults([])
      setTimeout(() => inputRef.current?.focus(), 0)
    }
  }, [isOpen])

  useEffect(() => {
    if (query.trim().length < 2) {
      setResults([])
      return
    }
    setSearching(true)
    const timeout = setTimeout(() => {
      accountApi.search(query).then(setResults).finally(() => setSearching(false))
    }, 250)
    return () => clearTimeout(timeout)
  }, [query])

  const handleSelect = (result: GlobalSearchResult) => {
    onOpenChange(false)
    navigate(`/teams/${result.teamId}/stories/${result.storyId}`)
  }

  if (!isOpen) return null

  return (
    <div className="modal-overlay" onClick={() => onOpenChange(false)} style={{ alignItems: 'flex-start', paddingTop: '15vh' }}>
      <div
        ref={containerRef}
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-label="Search across all teams"
        style={{ maxWidth: 560, width: '100%' }}
        onClick={(e) => e.stopPropagation()}
      >
        <input
          ref={inputRef}
          className="input"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search stories across all your teams…"
          style={{ fontSize: 15 }}
        />

        {isSearching && <p style={{ fontSize: 12.5, color: 'var(--color-ink-faint)', marginTop: 8 }}>Searching…</p>}

        {!isSearching && query.trim().length >= 2 && results.length === 0 && (
          <p style={{ fontSize: 13, marginTop: 12 }}>No matching stories.</p>
        )}

        {results.length > 0 && (
          <ul style={{ listStyle: 'none', margin: '12px 0 0', padding: 0, maxHeight: 360, overflowY: 'auto' }}>
            {results.map((result) => (
              <li key={result.storyId}>
                <button
                  onClick={() => handleSelect(result)}
                  style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%', textAlign: 'left', background: 'none', border: 'none', padding: '8px 4px', cursor: 'pointer', borderBottom: '1px solid var(--color-border)' }}
                >
                  <span>{result.title}</span>
                  <span className="mono" style={{ fontSize: 11, color: 'var(--color-ink-faint)' }}>{result.teamName} · {result.status}</span>
                </button>
              </li>
            ))}
          </ul>
        )}

        <p style={{ fontSize: 11, color: 'var(--color-ink-faint)', marginTop: 12 }}>
          Press <span className="mono">Esc</span> to close · <span className="mono">Ctrl/Cmd+K</span> to reopen
        </p>
      </div>
    </div>
  )
}
