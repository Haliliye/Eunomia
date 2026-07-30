import { useEffect, useState } from 'react'
import { invitationsApi } from '@/api/invitations'
import type { Invitation } from '@/types/invitation'
import { useToast } from '@/context/ToastContext'
import { useUserNames } from '@/hooks/useUserNames'
import { displayNameOrId } from '@/hooks/useUserNames'

interface PendingInvitationsProps {
  onAccepted: () => void
}

// Persistent counterpart to the notification bell's Accept/Decline — if the
// notification gets dismissed before it's acted on, the invitation isn't lost.
export default function PendingInvitations({ onAccepted }: PendingInvitationsProps) {
  const { showToast } = useToast()
  const [invitations, setInvitations] = useState<Invitation[]>([])
  const [isLoading, setLoading] = useState(true)
  const userNames = useUserNames(invitations.map((i) => i.invitedByUserId))

  const load = () => {
    invitationsApi.getMine().then(setInvitations).finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const handleAccept = async (invitation: Invitation) => {
    try {
      await invitationsApi.accept(invitation.id)
      setInvitations((prev) => prev.filter((i) => i.id !== invitation.id))
      showToast(`You joined "${invitation.teamName}".`)
      onAccepted()
    } catch {
      showToast('Could not accept that invitation.', 'error')
    }
  }

  const handleDecline = async (invitation: Invitation) => {
    try {
      await invitationsApi.decline(invitation.id)
      setInvitations((prev) => prev.filter((i) => i.id !== invitation.id))
    } catch {
      showToast('Could not decline that invitation.', 'error')
    }
  }

  if (isLoading || invitations.length === 0) return null

  return (
    <div className="card" style={{ borderColor: 'var(--color-brand)' }}>
      <div className="card-header">
        <h3>Pending invitations</h3>
      </div>
      <ul className="member-list">
        {invitations.map((invitation) => (
          <li className="member-row" key={invitation.id}>
            <span>
              <strong>{invitation.teamName}</strong> — invited by {displayNameOrId(userNames, invitation.invitedByUserId)}
            </span>
            <span style={{ display: 'flex', gap: 6 }}>
              <button className="btn btn-primary btn-sm" onClick={() => handleAccept(invitation)}>Accept</button>
              <button className="btn btn-sm" onClick={() => handleDecline(invitation)}>Decline</button>
            </span>
          </li>
        ))}
      </ul>
    </div>
  )
}
