import { useEffect, useState } from 'react'
import { Link, useOutletContext } from 'react-router-dom'
import { userStoriesApi, type TeamDashboard } from '@/api/userStories'
import { teamsApi } from '@/api/teams'
import type { Activity } from '@/types/activity'
import { Skeleton } from '@/components/common/Skeleton'
import { useUserNames, displayNameOrId, initialsFor } from '@/hooks/useUserNames'
import { avatarColor } from '@/lib/avatarColor'
import JiraSyncPanel from '@/components/teams/JiraSyncPanel'
import AzureDevOpsSyncPanel from '@/components/teams/AzureDevOpsSyncPanel'
import type { TeamOutletContext } from './TeamShellPage'

// A quick "how's this team doing" landing page — the first thing you see
// when opening a team, before drilling into the backlog/board/dashboard.
export default function TeamSummaryPage() {
  const { team } = useOutletContext<TeamOutletContext>()
  const [dashboard, setDashboard] = useState<TeamDashboard | null>(null)
  const [isLoading, setLoading] = useState(true)
  const [activity, setActivity] = useState<Activity[]>([])
  const [isActivityLoading, setActivityLoading] = useState(true)
  const owner = team.members.find((m) => m.role === 'Owner')
  const userNames = useUserNames([
    ...team.members.map((m) => m.userId),
    ...activity.map((a) => a.actorUserId),
  ])

  useEffect(() => {
    userStoriesApi.getDashboard(team.id).then(setDashboard).finally(() => setLoading(false))
  }, [team.id])

  useEffect(() => {
    teamsApi.getActivity(team.id, 1, 15).then((result) => setActivity(result.items)).finally(() => setActivityLoading(false))
  }, [team.id])

  const openCount = dashboard ? dashboard.totalCount - (dashboard.countsByStatus.Done ?? 0) : 0

  return (
    <div>
      <JiraSyncPanel team={team} />
      <AzureDevOpsSyncPanel team={team} />
      <div className="stat-grid">
        {isLoading ? (
          <>
            <Skeleton className="skeleton-tile" style={{ height: 72 }} />
            <Skeleton className="skeleton-tile" style={{ height: 72 }} />
            <Skeleton className="skeleton-tile" style={{ height: 72 }} />
          </>
        ) : (
          <>
            <div className="stat-tile">
              <div className="stat-value">{dashboard?.totalCount ?? 0}</div>
              <div className="stat-label">Total stories</div>
            </div>
            <div className="stat-tile">
              <div className="stat-value">{openCount}</div>
              <div className="stat-label">Open</div>
            </div>
            <div className="stat-tile">
              <div className="stat-value">{team.members.length}</div>
              <div className="stat-label">Members</div>
            </div>
          </>
        )}
      </div>

      <div className="card">
        <div className="card-header">
          <h3>Team</h3>
        </div>
        <p style={{ marginBottom: 8 }}>
          Owned by {owner ? displayNameOrId(userNames, owner.userId) : 'Unknown'}
        </p>
        <div style={{ display: 'flex', gap: 6 }}>
          {team.members.map((m) => (
            <span
              key={m.userId}
              className="backlog-avatar"
              style={{ background: avatarColor(m.userId), color: 'white' }}
              title={displayNameOrId(userNames, m.userId)}
            >
              {initialsFor(userNames, m.userId)}
            </span>
          ))}
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <h3>Recent activity</h3>
        </div>
        {isActivityLoading ? (
          <Skeleton style={{ height: 60 }} />
        ) : activity.length === 0 ? (
          <p>Nothing yet — activity shows up here as the team creates and updates stories.</p>
        ) : (
          <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
            {activity.map((a) => (
              <li key={a.id} style={{ padding: '6px 0', borderBottom: '1px solid var(--color-border)', fontSize: 13 }}>
                <strong>{displayNameOrId(userNames, a.actorUserId)}</strong> {a.message}
                <div className="mono" style={{ fontSize: 11, color: 'var(--color-ink-faint)' }}>
                  {new Date(a.createdOn).toLocaleString()}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="card">
        <div className="card-header">
          <h3>Jump to</h3>
        </div>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          <Link className="btn" to={`/teams/${team.id}/backlog`}>Backlog</Link>
          <Link className="btn" to={`/teams/${team.id}/board`}>Board</Link>
          <Link className="btn" to={`/teams/${team.id}/dashboard`}>Dashboard</Link>
          <Link className="btn" to={`/teams/${team.id}/members`}>Members</Link>
          <Link className="btn" to={`/teams/${team.id}/archived`}>Archived</Link>
        </div>
      </div>
    </div>
  )
}
