import { useEffect, useState } from 'react'
import { teamsApi } from '@/api/teams'
import type { Team } from '@/types/team'
import TeamList from '@/components/teams/TeamList'
import CreateTeamModal from '@/components/teams/CreateTeamModal'
import CreateTeamFromJiraModal from '@/components/teams/CreateTeamFromJiraModal'
import PendingInvitations from '@/components/teams/PendingInvitations'
import ConfirmDeleteModal from '@/components/common/ConfirmDeleteModal'
import { SkeletonTeamGrid } from '@/components/common/Skeleton'
import { useToast } from '@/context/ToastContext'
import { useAuth } from '@/context/AuthContext'
import { removeRecentTeam } from '@/lib/recentTeams'

const PAGE_SIZE = 25

export default function TeamsPage() {
  const { showToast } = useToast()
  const { user } = useAuth()
  const [teams, setTeams] = useState<Team[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [isModalOpen, setModalOpen] = useState(false)
  const [isJiraModalOpen, setJiraModalOpen] = useState(false)
  const [isLoading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [deletingTeam, setDeletingTeam] = useState<Team | null>(null)

  const load = (targetPage: number) => {
    setLoading(true)
    teamsApi.getMyTeams(targetPage, PAGE_SIZE)
      .then((result) => {
        setTeams(result.items)
        setTotalCount(result.totalCount)
        setPage(result.page)
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => { load(1) }, [])

  const handleCreate = async (name: string, description: string) => {
    await teamsApi.create(name, description)
    load(1)
    setModalOpen(false)
    showToast(`"${name}" was created.`)
  }

  const handleConfirmDelete = async () => {
    if (!deletingTeam) return
    const team = deletingTeam
    setDeletingTeam(null)

    try {
      await teamsApi.delete(team.id)
      if (user) removeRecentTeam(user.userId, team.id)
      load(page)
      setError(null)
      showToast(`"${team.name}" was deleted.`)
    } catch {
      setError('Could not delete this team — only the owner can delete it.')
    }
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))

  return (
    <section>
      <div className="page-header">
        <div>
          <span className="page-header-eyebrow">Workspace</span>
          <h1>My Teams</h1>
        </div>
        <div className="page-header-actions">
          <button className="btn" onClick={() => setJiraModalOpen(true)}>Import from Jira</button>
          <button className="btn btn-primary" onClick={() => setModalOpen(true)}>+ New Team</button>
        </div>
      </div>

      {error && <div className="alert-error" role="alert">{error}</div>}
      <PendingInvitations onAccepted={() => load(page)} />
      {isLoading ? <SkeletonTeamGrid /> : <TeamList teams={teams} onDelete={setDeletingTeam} />}

      {totalPages > 1 && (
        <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginTop: 16 }}>
          <button className="btn btn-sm" disabled={page <= 1} onClick={() => load(page - 1)}>← Prev</button>
          <span className="mono" style={{ fontSize: 12.5 }}>Page {page} of {totalPages}</span>
          <button className="btn btn-sm" disabled={page >= totalPages} onClick={() => load(page + 1)}>Next →</button>
        </div>
      )}

      <CreateTeamModal
        isOpen={isModalOpen}
        onClose={() => setModalOpen(false)}
        onCreate={handleCreate}
      />
      {isJiraModalOpen && <CreateTeamFromJiraModal onClose={() => setJiraModalOpen(false)} />}
      <ConfirmDeleteModal
        isOpen={deletingTeam !== null}
        title="Delete team"
        description={`This will also permanently delete every user story in "${deletingTeam?.name}". This can't be undone.`}
        confirmationText={deletingTeam?.name ?? ''}
        onClose={() => setDeletingTeam(null)}
        onConfirm={handleConfirmDelete}
      />
    </section>
  )
}
