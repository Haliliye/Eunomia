import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'
import { avatarColor } from '@/lib/avatarColor'

function initials(name: string): string {
  const parts = name.trim().split(/\s+/)
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

interface UserMenuProps {
  onLogout: () => void
}

export default function UserMenu({ onLogout }: UserMenuProps) {
  const { user } = useAuth()
  const [isOpen, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isOpen) return
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [isOpen])

  if (!user) return null

  return (
    <div ref={containerRef} style={{ position: 'relative' }}>
      <button
        onClick={() => setOpen((prev) => !prev)}
        aria-label="Account menu"
        title={user.displayName}
        style={{
          width: 32, height: 32, borderRadius: '50%', border: 'none', cursor: 'pointer',
          background: avatarColor(user.userId), color: '#fff', fontSize: 12.5, fontWeight: 600,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
        }}
      >
        {initials(user.displayName)}
      </button>

      {isOpen && (
        <div className="notif-panel" style={{ right: 0, left: 'auto', width: 220 }}>
          <div style={{ fontSize: 13, fontWeight: 500, marginBottom: 2 }}>{user.displayName}</div>
          <div className="mono" style={{ fontSize: 11, color: 'var(--color-ink-faint)', marginBottom: 10 }}>{user.email}</div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Link to="/settings" className="sidebar-link" onClick={() => setOpen(false)}>
              <span className="sidebar-link-icon" aria-hidden="true">⚙️</span> Settings
            </Link>
            <button
              onClick={() => { setOpen(false); onLogout() }}
              className="sidebar-link"
              style={{ background: 'none', border: 'none', width: '100%', textAlign: 'left', cursor: 'pointer' }}
            >
              <span className="sidebar-link-icon" aria-hidden="true">🚪</span> Log out
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
