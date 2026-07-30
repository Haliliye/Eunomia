import { useEffect, useState } from 'react'
import { useOutletContext, Link } from 'react-router-dom'
import { userStoriesApi, type TeamDashboard } from '@/api/userStories'
import { teamsApi, type TeamTimeReport } from '@/api/teams'
import { sprintsApi, type SprintBurndown, type VelocityPoint } from '@/api/sprints'
import type { Sprint } from '@/types/sprint'
import BurndownChart from '@/components/sprints/BurndownChart'
import VelocityChart from '@/components/sprints/VelocityChart'
import { Skeleton } from '@/components/common/Skeleton'
import { useUserNames, displayNameOrId } from '@/hooks/useUserNames'
import type { TeamOutletContext } from './TeamShellPage'

const STATUS_LABELS: Record<string, string> = {
  ToDo: 'To Do',
  Analyze: 'Analyze',
  Dev: 'Dev',
  Test: 'Test',
  Debug: 'Debug',
  Done: 'Done',
}

const STATUS_COLOR: Record<string, string> = {
  ToDo: 'var(--color-todo)',
  Analyze: 'var(--color-analyze)',
  Dev: 'var(--color-dev)',
  Test: 'var(--color-test)',
  Debug: 'var(--color-debug)',
  Done: 'var(--color-done)',
}

export default function DashboardPage() {
  const { team } = useOutletContext<TeamOutletContext>()
  const [dashboard, setDashboard] = useState<TeamDashboard | null>(null)
  const [timeReport, setTimeReport] = useState<TeamTimeReport | null>(null)
  const [sprints, setSprints] = useState<Sprint[]>([])
  const [sprintFilter, setSprintFilter] = useState('')
  const [burndown, setBurndown] = useState<SprintBurndown | null>(null)
  const [velocity, setVelocity] = useState<VelocityPoint[]>([])
  const [isLoading, setLoading] = useState(true)
  const userNames = useUserNames(Object.keys(dashboard?.countsByAssignee ?? {}))

  useEffect(() => {
    sprintsApi.getForTeam(team.id).then(setSprints)
    sprintsApi.getVelocity(team.id).then(setVelocity)
  }, [team.id])

  useEffect(() => {
    setLoading(true)
    userStoriesApi.getDashboard(team.id, sprintFilter || undefined)
      .then(setDashboard)
      .finally(() => setLoading(false))
    teamsApi.getTimeReport(team.id).then(setTimeReport)

    // A burndown chart only makes sense for one specific sprint, not "whole team".
    if (sprintFilter) {
      sprintsApi.getBurndown(sprintFilter).then(setBurndown).catch(() => setBurndown(null))
    } else {
      setBurndown(null)
    }
  }, [team.id, sprintFilter])

  if (isLoading) {
    return (
      <div role="status" aria-label="Loading dashboard">
        <div className="stat-grid">
          <Skeleton className="skeleton-tile" style={{ height: 72 }} />
          <Skeleton className="skeleton-tile" style={{ height: 72 }} />
          <Skeleton className="skeleton-tile" style={{ height: 72 }} />
        </div>
        <Skeleton style={{ height: 120, borderRadius: 12 }} />
      </div>
    )
  }
  if (!dashboard) return <p>No data.</p>

  const maxStatusCount = Math.max(1, ...Object.values(dashboard.countsByStatus))
  const maxAssigneeCount = Math.max(1, ...Object.values(dashboard.countsByAssignee), 1)
  const openCount = Object.values(dashboard.countsByAssignee).reduce((a, b) => a + b, 0)

  return (
    <div>
      <div className="print-report-header">
        <h1 style={{ margin: 0 }}>{team.name} — Dashboard Report</h1>
        <p style={{ fontSize: 12, color: '#666' }}>Generated {new Date().toLocaleString()}</p>
      </div>

      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 8 }}>
        <button className="btn btn-sm" onClick={() => window.print()}>🖨️ Print / Export as PDF</button>
      </div>

      {sprints.length > 0 && (
        <div style={{ marginBottom: 16 }}>
          <select className="pill-select" value={sprintFilter} onChange={(e) => setSprintFilter(e.target.value)}>
            <option value="">Whole team (all sprints)</option>
            {sprints.map((s) => (
              <option key={s.id} value={s.id}>{s.name} ({s.status})</option>
            ))}
          </select>
        </div>
      )}

      <div className="stat-grid">
        <div className="stat-tile">
          <div className="stat-value">{dashboard.totalCount}</div>
          <div className="stat-label">Total stories</div>
        </div>
        <div className="stat-tile">
          <div className="stat-value">{dashboard.countsByStatus.Done ?? 0}</div>
          <div className="stat-label">Done</div>
        </div>
        <div className="stat-tile">
          <div className="stat-value">{openCount}</div>
          <div className="stat-label">Open items</div>
        </div>
      </div>

      {burndown && (
        <div className="card">
          <div className="card-header">
            <h2>Burndown</h2>
            <span className="mono" style={{ fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
              {burndown.totalPointsAtStart} pts planned at start
            </span>
          </div>
          <BurndownChart burndown={burndown} />
          <p style={{ fontSize: 11.5, color: 'var(--color-ink-faint)', marginTop: 4 }}>
            Dashed line: ideal pace. Solid line: actual remaining points, snapshotted once per day.
          </p>
        </div>
      )}

      {velocity.length > 0 && (
        <div className="card">
          <div className="card-header"><h2>Team velocity</h2></div>
          <VelocityChart points={velocity} />
          <p style={{ fontSize: 11.5, color: 'var(--color-ink-faint)', marginTop: 4 }}>
            Grey bars: points planned at sprint start. Blue bars: points actually completed.
            Dashed line: average completed across shown sprints.
          </p>
        </div>
      )}

      <div className="card">
        <h2>By status</h2>
        {Object.entries(dashboard.countsByStatus).map(([status, count]) => (
          <div className="dashboard-metric-row" key={status}>
            <span className="dashboard-label">{STATUS_LABELS[status] ?? status}</span>
            <div className="dashboard-track">
              <div
                className="dashboard-fill"
                style={{ width: `${(count / maxStatusCount) * 100}%`, background: STATUS_COLOR[status] ?? 'var(--color-brand)' }}
              />
            </div>
            <span className="dashboard-count">{count}</span>
          </div>
        ))}
      </div>

      <div className="card">
        <h2>Open items by assignee</h2>
        {Object.keys(dashboard.countsByAssignee).length === 0 ? (
          <p>No open items.</p>
        ) : (
          Object.entries(dashboard.countsByAssignee).map(([assignee, count]) => (
            <div className="dashboard-metric-row" key={assignee}>
              <span className="dashboard-label">
                {assignee === 'Unassigned' ? 'Unassigned' : displayNameOrId(userNames, assignee)}
              </span>
              <div className="dashboard-track">
                <div className="dashboard-fill" style={{ width: `${(count / maxAssigneeCount) * 100}%`, background: 'var(--color-brand)' }} />
              </div>
              <span className="dashboard-count">{count}</span>
            </div>
          ))
        )}
      </div>

      {timeReport && timeReport.rows.length > 0 && (
        <div className="card">
          <div className="card-header">
            <h2>Time report — estimate vs. actual</h2>
            <span className="mono" style={{ fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
              {timeReport.totalLoggedHours}h logged / {timeReport.totalEstimatedHours}h estimated
            </span>
          </div>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
            <thead>
              <tr style={{ textAlign: 'left', borderBottom: '1px solid var(--color-border)' }}>
                <th style={{ padding: '6px 0' }}>Story</th>
                <th style={{ padding: '6px 0' }}>Estimate</th>
                <th style={{ padding: '6px 0' }}>Logged</th>
                <th style={{ padding: '6px 0' }}>Variance</th>
              </tr>
            </thead>
            <tbody>
              {timeReport.rows.map((row) => (
                <tr key={row.storyId} style={{ borderBottom: '1px solid var(--color-border)' }}>
                  <td style={{ padding: '6px 0' }}>
                    <Link to={`/teams/${team.id}/stories/${row.storyId}`}>{row.title}</Link>
                  </td>
                  <td style={{ padding: '6px 0' }} className="mono">{row.estimatedHours ?? '—'}</td>
                  <td style={{ padding: '6px 0' }} className="mono">{row.loggedHours}</td>
                  <td style={{ padding: '6px 0' }} className="mono">
                    {row.variance !== undefined ? (
                      <span style={{ color: row.variance > 0 ? 'var(--color-danger)' : 'var(--color-done)' }}>
                        {row.variance > 0 ? '+' : ''}{row.variance}h
                      </span>
                    ) : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
