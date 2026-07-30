import { useEffect, useState } from 'react'
import type { Team } from '@/types/team'
import type { Invitation } from '@/types/invitation'
import { invitationsApi } from '@/api/invitations'
import { teamsApi } from '@/api/teams'
import { useUserNames, displayNameOrId } from '@/hooks/useUserNames'
import { useToast } from '@/context/ToastContext'

interface TeamMembersProps {
  team: Team
  currentUserId: string
  userNames: Record<string, string>
  onInvite: (email: string) => void
  onRemoveMember: (userId: string) => void
  onRoleChanged?: () => void
}

export default function TeamMembers({ team, currentUserId, userNames, onInvite, onRemoveMember, onRoleChanged }: TeamMembersProps) {
  const { showToast } = useToast()
  const [inviteEmail, setInviteEmail] = useState('')
  const [pendingInvitations, setPendingInvitations] = useState<Invitation[]>([])
  const isOwner = team.members.some((m) => m.userId === currentUserId && m.role === 'Owner')
  const invitedUserNames = useUserNames(pendingInvitations.map((i) => i.invitedUserId))

  const loadPendingInvitations = () => {
    if (!isOwner) return
    invitationsApi.getForTeam(team.id).then(setPendingInvitations)
  }

  useEffect(() => {
    loadPendingInvitations()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [team.id, isOwner])

  const handleAdd = () => {
    if (!inviteEmail.trim()) return
    onInvite(inviteEmail.trim())
    setInviteEmail('')
    // The invite was just sent — refresh the pending list after a beat so
    // the new one (created server-side) shows up here too.
    setTimeout(loadPendingInvitations, 300)
  }

  const handleCancel = async (invitation: Invitation) => {
    try {
      await invitationsApi.cancel(invitation.id)
      setPendingInvitations((prev) => prev.filter((i) => i.id !== invitation.id))
      showToast('Invitation cancelled.')
    } catch {
      showToast('Could not cancel that invitation.', 'error')
    }
  }

  const handleSetRole = async (userId: string, role: 'Admin' | 'Member') => {
    try {
      await teamsApi.setMemberRole(team.id, userId, role)
      onRoleChanged?.()
      showToast(role === 'Admin' ? 'Member promoted to Admin.' : 'Admin demoted to Member.')
    } catch {
      showToast('Could not change that member\'s role.', 'error')
    }
  }

  return (
    <div className="card">
      <div className="card-header">
        <h3>Members</h3>
      </div>
      <ul className="member-list">
        {team.members.map((member) => (
          <li className="member-row" key={member.userId}>
            <span>
              {displayNameOrId(userNames, member.userId)}
              <span className="member-role">{member.role}</span>
            </span>
            {isOwner && member.role !== 'Owner' && (
              <span style={{ display: 'flex', gap: 6 }}>
                {member.role === 'Member' ? (
                  <button className="btn btn-ghost btn-sm" onClick={() => handleSetRole(member.userId, 'Admin')}>Make admin</button>
                ) : (
                  <button className="btn btn-ghost btn-sm" onClick={() => handleSetRole(member.userId, 'Member')}>Remove admin</button>
                )}
                <button className="btn btn-ghost btn-sm" onClick={() => onRemoveMember(member.userId)}>Remove</button>
              </span>
            )}
          </li>
        ))}
      </ul>

      {isOwner && pendingInvitations.length > 0 && (
        <>
          <h3 style={{ marginTop: 16 }}>Pending invitations</h3>
          <ul className="member-list">
            {pendingInvitations.map((invitation) => (
              <li className="member-row" key={invitation.id}>
                <span style={{ color: 'var(--color-ink-muted)' }}>
                  {displayNameOrId(invitedUserNames, invitation.invitedUserId)}
                  <span className="member-role">Pending</span>
                </span>
                <button className="btn btn-ghost btn-sm" onClick={() => handleCancel(invitation)}>Cancel</button>
              </li>
            ))}
          </ul>
        </>
      )}

      {isOwner && (
        <div className="member-add-row">
          <input
            className="input"
            type="email"
            value={inviteEmail}
            onChange={(e) => setInviteEmail(e.target.value)}
            placeholder="Email of an existing account"
            style={{ flex: 1 }}
          />
          <button className="btn btn-sm" onClick={handleAdd}>Send invite</button>
        </div>
      )}
    </div>
  )
}
