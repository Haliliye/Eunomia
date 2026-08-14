import { useEffect, useState } from 'react'
import { Link, Outlet, useLocation, useNavigate, useParams } from 'react-router-dom'
import NotificationBell from '@/components/notifications/NotificationBell'
import UserMenu from '@/components/common/UserMenu'
import CommandPalette from '@/components/common/CommandPalette'
import { useTheme } from '@/hooks/useTheme'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/context/ToastContext'
import { teamsApi } from '@/api/teams'
import { authApi } from '@/api/auth'
import type { Team } from '@/types/team'
import { getRecentTeams, removeRecentTeam, type RecentTeam } from '@/lib/recentTeams'

export default function Layout() {
  const [isSearchOpen, setSearchOpen] = useState(false)
  const location = useLocation()
  const navigate = useNavigate()
  const { teamId } = useParams<{ teamId: string }>()
  const isTeamsActive = location.pathname === '/teams'
  const isBoardTab = location.pathname.endsWith('/board')
  const { theme, toggleTheme } = useTheme()
  const { user, logout } = useAuth()
  const { showToast } = useToast()
  const [teams, setTeams] = useState<Team[]>([])
  const [recentTeams, setRecentTeams] = useState<RecentTeam[]>([])
  const [isResending, setResending] = useState(false)
  const [devVerificationToken, setDevVerificationToken] = useState<string | null>(null)

  useEffect(() => {
    // A generous page size — this is a "quick switch" list in the sidebar,
    // not the full paginated /teams page, so we just show as many as
    // reasonably fit without adding pagination controls here too.
    teamsApi.getMyTeams(1, 50).then((result) => setTeams(result.items))
  }, [])

  useEffect(() => {
    // Refreshed on every navigation so a team visited just now shows up
    // immediately, not just after a full page reload. Also re-runs when the
    // logged-in user changes, so switching accounts on the same browser
    // shows that account's own recent teams, not the previous one's.
    if (!user) { setRecentTeams([]); return }

    const stored = getRecentTeams(user.userId)
    if (teams.length === 0) { setRecentTeams(stored); return }

    // A team that no longer exists (deleted before the fix that keeps
    // Recent in sync, or removed by someone else) can otherwise linger in
    // this localStorage cache indefinitely — prune it here against the
    // teams the person actually still has, and persist the prune so it
    // doesn't need repeating every navigation.
    const myTeamIds = new Set(teams.map((t) => t.id))
    const valid = stored.filter((t) => myTeamIds.has(t.id))
    if (valid.length !== stored.length) {
      stored.filter((t) => !myTeamIds.has(t.id)).forEach((t) => removeRecentTeam(user.userId, t.id))
    }
    setRecentTeams(valid)
  }, [location.pathname, user?.userId, teams])

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const handleResendVerification = async () => {
    setResending(true)
    try {
      const result = await authApi.resendVerification()
      showToast(result.message)
      if (result.devVerificationToken) {
        setDevVerificationToken(result.devVerificationToken)
      }
    } finally {
      setResending(false)
    }
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-logo">
          <img src="/logo.png" alt="" className="sidebar-logo-mark" />
          Eunomia
        </div>

        <nav className="sidebar-nav">
          <button className="sidebar-link" onClick={() => setSearchOpen(true)} style={{ background: 'none', border: 'none', width: '100%', textAlign: 'left', cursor: 'pointer' }}>
            <span className="sidebar-link-icon" aria-hidden="true">🔍</span> Search <span className="mono" style={{ marginLeft: 'auto', fontSize: 11, color: 'var(--color-ink-faint)' }}>Ctrl+K</span>
          </button>
          <Link to="/my-work" className={`sidebar-link ${location.pathname === '/my-work' ? 'active' : ''}`}>
            <span className="sidebar-link-icon" aria-hidden="true">✅</span> My Work
          </Link>
          <Link to="/my-tasks" className={`sidebar-link ${location.pathname === '/my-tasks' ? 'active' : ''}`}>
            <span className="sidebar-link-icon" aria-hidden="true">📝</span> My Tasks
          </Link>
          <Link to="/teams" className={`sidebar-link ${isTeamsActive ? 'active' : ''}`}>
            <span className="sidebar-link-icon" aria-hidden="true">⊞</span> All Teams
          </Link>
          <Link to="/portfolio" className={`sidebar-link ${location.pathname === '/portfolio' ? 'active' : ''}`}>
            <span className="sidebar-link-icon" aria-hidden="true">📊</span> Portfolio
          </Link>
        </nav>

        {recentTeams.length > 0 && (
          <>
            <div className="sidebar-section-label">
              <span aria-hidden="true">🕐</span> Recent
            </div>
            <nav className="sidebar-nav">
              {recentTeams.map((team) => (
                <Link
                  key={team.id}
                  to={`/teams/${team.id}`}
                  className={`sidebar-team-link ${teamId === team.id ? 'active' : ''}`}
                  title={team.name}
                >
                  {team.name}
                </Link>
              ))}
            </nav>
          </>
        )}

        {teams.length > 0 && (
          <>
            <div className="sidebar-section-label">
              <span aria-hidden="true">◫</span> Your Teams
            </div>
            <nav className="sidebar-nav">
              {teams.map((team) => (
                <Link
                  key={team.id}
                  to={`/teams/${team.id}`}
                  className={`sidebar-team-link ${teamId === team.id ? 'active' : ''}`}
                  title={team.name}
                >
                  {team.name}
                </Link>
              ))}
            </nav>
          </>
        )}
      </aside>

      <div className="main-area">
        <div className="topbar">
          <button
            className="theme-toggle"
            onClick={toggleTheme}
            aria-label={theme === 'light' ? 'Switch to dark mode' : 'Switch to light mode'}
            title={theme === 'light' ? 'Switch to dark mode' : 'Switch to light mode'}
          >
            {theme === 'light' ? '🌙' : '☀️'}
          </button>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <NotificationBell />
            <UserMenu onLogout={handleLogout} />
          </div>
        </div>

        {user && !user.isEmailVerified && (
          <div className="alert-error" style={{ margin: '0 32px', background: 'var(--color-brand-soft)', color: 'var(--color-brand-ink)', borderColor: 'var(--color-brand)' }}>
            Verify your email ({user.email}) to secure your account.{' '}
            <button className="btn btn-sm" onClick={handleResendVerification} disabled={isResending} style={{ marginLeft: 8 }}>
              {isResending ? 'Sending…' : 'Resend verification link'}
            </button>
            {(devVerificationToken || user.emailVerificationDevToken) && (
              <div style={{ marginTop: 6, fontSize: 12.5 }}>
                <strong>No SMTP configured:</strong>{' '}
                <Link to={`/verify-email?token=${devVerificationToken ?? user.emailVerificationDevToken}`}>
                  Verify email
                </Link>
              </div>
            )}
          </div>
        )}

        <div className={`content ${isBoardTab ? 'content-wide' : ''}`}>
          <Outlet />
        </div>
      </div>

      <CommandPalette isOpen={isSearchOpen} onOpenChange={setSearchOpen} />
    </div>
  )
}
