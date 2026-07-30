import { useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { teamsApi } from '@/api/teams'
import type { Activity, ActivityType } from '@/types/activity'
import { useUserNames, displayNameOrId } from '@/hooks/useUserNames'
import { SkeletonTable } from '@/components/common/Skeleton'
import type { TeamOutletContext } from './TeamShellPage'

const PAGE_SIZE = 25
const ACTION_TYPES: ActivityType[] = ['Created', 'StatusChanged', 'Assigned', 'Archived', 'Commented']

// US-132: team-wide activity feed, paginated. US-133: filterable by actor and/or action type.
export default function TeamActivityPage() {
  const { team } = useOutletContext<TeamOutletContext>()
  const [activities, setActivities] = useState<Activity[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [isLoading, setLoading] = useState(true)
  const [actorFilter, setActorFilter] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const userNames = useUserNames([...team.members.map((m) => m.userId), ...activities.map((a) => a.actorUserId)])

  const load = (targetPage: number) => {
    setLoading(true)
    teamsApi.getActivity(team.id, targetPage, PAGE_SIZE, actorFilter || undefined, typeFilter || undefined)
      .then((result) => {
        setActivities(result.items)
        setTotalCount(result.totalCount)
        setPage(result.page)
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    load(1)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [team.id, actorFilter, typeFilter])

  return (
    <div>
      <div className="filter-bar" style={{ marginBottom: 16 }}>
        <select className="pill-select" value={actorFilter} onChange={(e) => setActorFilter(e.target.value)}>
          <option value="">All members</option>
          {team.members.map((m) => (
            <option key={m.userId} value={m.userId}>{displayNameOrId(userNames, m.userId)}</option>
          ))}
        </select>
        <select className="pill-select" value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
          <option value="">All action types</option>
          {ACTION_TYPES.map((t) => (
            <option key={t} value={t}>{t}</option>
          ))}
        </select>
        {(actorFilter || typeFilter) && (
          <button className="btn btn-sm" onClick={() => { setActorFilter(''); setTypeFilter('') }}>Clear filters</button>
        )}
      </div>

      {isLoading ? (
        <SkeletonTable />
      ) : activities.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-title">No activity found</div>
          <p>Try clearing your filters, or check back once the team starts working.</p>
        </div>
      ) : (
        <>
          <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
            {activities.map((a) => (
              <li key={a.id} style={{ padding: '10px 0', borderBottom: '1px solid var(--color-border)', fontSize: 13.5 }}>
                <strong>{displayNameOrId(userNames, a.actorUserId)}</strong> {a.message}
                <div className="mono" style={{ fontSize: 11, color: 'var(--color-ink-faint)' }}>
                  {a.type} · {new Date(a.createdOn).toLocaleString()}
                </div>
              </li>
            ))}
          </ul>

          {totalCount > PAGE_SIZE && (
            <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginTop: 16 }}>
              <button className="btn btn-sm" disabled={page <= 1} onClick={() => load(page - 1)}>← Prev</button>
              <span className="mono" style={{ fontSize: 12.5 }}>
                {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, totalCount)} of {totalCount}
              </span>
              <button className="btn btn-sm" disabled={page * PAGE_SIZE >= totalCount} onClick={() => load(page + 1)}>Next →</button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
