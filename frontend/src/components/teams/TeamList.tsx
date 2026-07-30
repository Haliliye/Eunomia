import { Link } from 'react-router-dom'
import type { Team } from '@/types/team'

interface TeamListProps {
  teams: Team[]
  onDelete: (team: Team) => void
}

export default function TeamList({ teams, onDelete }: TeamListProps) {
  if (teams.length === 0) {
    return (
      <div className="empty-state">
        <div className="empty-state-title">No teams yet</div>
        <p>Create your first team to start tracking work together.</p>
      </div>
    )
  }

  return (
    <div className="team-grid">
      {teams.map((team) => (
        <div className="team-tile" key={team.id}>
          <button
            className="btn btn-ghost btn-sm team-tile-delete"
            onClick={(e) => {
              e.preventDefault()
              onDelete(team)
            }}
            aria-label={`Delete ${team.name}`}
          >
            ✕
          </button>
          <Link to={`/teams/${team.id}`} style={{ textDecoration: 'none' }}>
            <span className="team-tile-name">{team.name}</span>
          </Link>
          <div className="team-tile-desc">{team.description || 'No description'}</div>
          <div className="team-tile-footer">
            <span className="mono">{team.members.length} member{team.members.length === 1 ? '' : 's'}</span>
            <Link to={`/teams/${team.id}`}>Open →</Link>
          </div>
        </div>
      ))}
    </div>
  )
}
