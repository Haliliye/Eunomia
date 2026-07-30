import { useOutletContext } from 'react-router-dom'
import { teamsApi } from '@/api/teams'
import TeamMembers from '@/components/teams/TeamMembers'
import LabelManager from '@/components/teams/LabelManager'
import WipLimitsManager from '@/components/teams/WipLimitsManager'
import TemplateManager from '@/components/teams/TemplateManager'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/context/ToastContext'
import { useUserNames } from '@/hooks/useUserNames'
import type { TeamOutletContext } from './TeamShellPage'

export default function TeamMembersPage() {
  const { team, reloadTeam } = useOutletContext<TeamOutletContext>()
  const { user } = useAuth()
  const { showToast } = useToast()
  const userNames = useUserNames(team.members.map((m) => m.userId))

  if (!user) return null

  const handleInvite = async (email: string) => {
    try {
      await teamsApi.invite(team.id, email)
      showToast(`Invitation sent to ${email}.`)
    } catch (err) {
      showToast(extractErrorMessage(err), 'error')
    }
  }

  const handleRemoveMember = async (userId: string) => {
    try {
      await teamsApi.removeMember(team.id, userId)
      reloadTeam()
      showToast(`${userId} was removed from the team.`)
    } catch (err) {
      showToast(extractErrorMessage(err), 'error')
    }
  }

  return (
    <div>
      <TeamMembers
        team={team}
        currentUserId={user.userId}
        userNames={userNames}
        onInvite={handleInvite}
        onRemoveMember={handleRemoveMember}
        onRoleChanged={reloadTeam}
      />
      <LabelManager team={team} isOwner={team.members.some((m) => m.userId === user.userId && m.role === 'Owner')} onChanged={reloadTeam} />
      <WipLimitsManager team={team} isOwner={team.members.some((m) => m.userId === user.userId && m.role === 'Owner')} onChanged={reloadTeam} />
      <TemplateManager team={team} isOwner={team.members.some((m) => m.userId === user.userId && m.role === 'Owner')} onChanged={reloadTeam} />
    </div>
  )
}

function extractErrorMessage(err: unknown): string {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const response = (err as { response?: { data?: { error?: string } } }).response
    if (response?.data?.error) return response.data.error
  }
  return 'Something went wrong. Please try again.'
}
