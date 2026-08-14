import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { teamsApi, type TeamPortfolioSummary } from '@/api/teams'
import { SkeletonTable } from '@/components/common/Skeleton'

// A single "how's everything doing" view for someone who's a member of
// several teams — one row per team instead of opening each one's dashboard.
export default function PortfolioPage() {
  const [rows, setRows] = useState<TeamPortfolioSummary[]>([])
  const [isLoading, setLoading] = useState(true)

  useEffect(() => {
    teamsApi.getPortfolio().then(setRows).finally(() => setLoading(false))
  }, [])

  if (isLoading) {
    return (
      <section>
        <h1>Portfolio</h1>
        <SkeletonTable rows={4} />
      </section>
    )
  }

  if (rows.length === 0) {
    return (
      <section>
        <h1>Portfolio</h1>
        <div className="empty-state">
          <div className="empty-state-title">No teams yet</div>
          <p>Join or create a team to see it here.</p>
        </div>
      </section>
    )
  }

  return (
    <section>
      <h1 style={{ marginBottom: 4 }}>Portfolio</h1>
      <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 16 }}>
        Where each of your {rows.length} team{rows.length === 1 ? '' : 's'} stands right now.
      </p>

      <div className="backlog-list">
        {rows.map((row) => {
          const completionPct = row.totalStoryCount > 0 ? Math.round((row.doneCount / row.totalStoryCount) * 100) : 0
          const sprintOverdue = row.activeSprintEndDate ? new Date(row.activeSprintEndDate).getTime() < Date.now() : false

          return (
            <Link
              key={row.teamId}
              to={`/teams/${row.teamId}`}
              className="backlog-row"
              style={{ textDecoration: 'none', color: 'inherit', padding: '12px 16px' }}
            >
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ fontWeight: 600, fontSize: 14 }}>{row.teamName}</div>
                <div style={{ fontSize: 11.5, color: 'var(--color-ink-faint)' }}>
                  {row.memberCount} member{row.memberCount === 1 ? '' : 's'}
                </div>
              </div>

              <div style={{ width: 120 }}>
                <div className="mono" style={{ fontSize: 11.5, marginBottom: 3 }}>
                  {row.doneCount}/{row.totalStoryCount} done
                </div>
                <div style={{ height: 5, borderRadius: 3, background: 'var(--color-surface-sunken)', overflow: 'hidden' }}>
                  <div style={{ height: '100%', width: `${completionPct}%`, background: 'var(--color-done)' }} />
                </div>
              </div>

              {row.overdueCount > 0 ? (
                <span className="badge" style={{ background: 'var(--color-danger-bg)', color: 'var(--color-danger)' }}>
                  {row.overdueCount} overdue
                </span>
              ) : (
                <span style={{ width: 90 }} />
              )}

              <div style={{ width: 170, fontSize: 12 }}>
                {row.activeSprintName ? (
                  <span style={{ color: sprintOverdue ? 'var(--color-danger)' : 'var(--color-ink-muted)' }}>
                    {row.activeSprintName} · ends {new Date(row.activeSprintEndDate!).toLocaleDateString()}
                  </span>
                ) : (
                  <span style={{ color: 'var(--color-ink-faint)' }}>No active sprint</span>
                )}
              </div>
            </Link>
          )
        })}
      </div>
    </section>
  )
}
