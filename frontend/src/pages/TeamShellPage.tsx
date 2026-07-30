import { useEffect, useState } from 'react'
import { useParams, Link, Outlet } from 'react-router-dom'
import { teamsApi } from '@/api/teams'
import type { Team } from '@/types/team'
import TeamTabs from '@/components/teams/TeamTabs'
import { Skeleton } from '@/components/common/Skeleton'
import { recordRecentTeam } from '@/lib/recentTeams'
import { useAuth } from '@/context/AuthContext'

// Shell for every /teams/:teamId/* page: fetches the team once, renders the
// header + tab bar, and hands the team down to whichever tab is active via
// Outlet context — so BoardPage/DashboardPage/etc don't each need their own
// "fetch this team" boilerplate just to know its name.
export default function TeamShellPage() {
  const { teamId } = useParams<{ teamId: string }>()
  const { user } = useAuth()
  const [team, setTeam] = useState<Team | null>(null)
  const [isLoading, setLoading] = useState(true)

  const reloadTeam = () => {
    if (!teamId) return
    teamsApi.getById(teamId).then(setTeam)
  }

  useEffect(() => {
    if (!teamId) return
    setLoading(true)
    teamsApi.getById(teamId)
      .then((data) => {
        setTeam(data)
        if (user) recordRecentTeam(user.userId, data.id, data.name)
      })
      .finally(() => setLoading(false))
  }, [teamId])

  if (isLoading || !team) {
    return (
      <section>
        <Skeleton className="skeleton-title" />
        <Skeleton style={{ height: 32, marginBottom: 16 }} />
      </section>
    )
  }

  return (
    <section>
      <div className="breadcrumb"><Link to="/teams">← My Teams</Link></div>
      <div className="page-header">
        <div>
          <span className="page-header-eyebrow">Team</span>
          <h1>{team.name}</h1>
          {team.description && <p style={{ marginTop: 4 }}>{team.description}</p>}
        </div>
      </div>

      <TeamTabs teamId={team.id} />

      <Outlet context={{ team, reloadTeam } satisfies TeamOutletContext} />
    </section>
  )
}

export interface TeamOutletContext {
  team: Team
  reloadTeam: () => void
}
