import { useEffect, useState } from 'react'
import { integrationsApi, type JiraSyncStatus } from '@/api/integrations'
import type { Team } from '@/types/team'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/context/ToastContext'

interface JiraSyncPanelProps {
  team: Team
}

export default function JiraSyncPanel({ team }: JiraSyncPanelProps) {
  const { user } = useAuth()
  const { showToast } = useToast()
  const [status, setStatus] = useState<JiraSyncStatus | null>(null)
  const [isSyncing, setSyncing] = useState(false)
  const [isTogglingSync, setTogglingSync] = useState(false)

  const isOwnerOrAdmin = team.members.some((m) => m.userId === user?.userId && (m.role === 'Owner' || m.role === 'Admin'))

  const load = () => integrationsApi.getJiraSyncStatus(team.id).then(setStatus).catch(() => setStatus(null))

  useEffect(load, [team.id])

  if (!status?.isLinked) return null // not every team came from Jira — nothing to show for the rest

  const handleSyncNow = async () => {
    setSyncing(true)
    try {
      const summary = await integrationsApi.syncJiraTeamNow(team.id)
      showToast(`Synced: ${summary.createdCount} created, ${summary.updatedCount} updated.`)
      load()
    } catch {
      showToast("Sync failed. Your Jira connection may have expired — try reconnecting from Settings.", 'error')
    } finally {
      setSyncing(false)
    }
  }

  const handleToggleAutoSync = async (enabled: boolean) => {
    setTogglingSync(true)
    try {
      await integrationsApi.setJiraAutoSync(team.id, enabled)
      load()
    } catch {
      showToast("Couldn't update the auto-sync setting.", 'error')
    } finally {
      setTogglingSync(false)
    }
  }

  return (
    <div className="card" style={{ marginBottom: 20 }}>
      <div className="card-header"><h3>Jira</h3></div>
      <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 8 }}>
        Linked to Jira project <strong>{status.projectKey}</strong>.{' '}
        {status.lastSyncedOn ? `Last synced ${new Date(status.lastSyncedOn).toLocaleString()}.` : 'Never synced yet.'}
      </p>

      {isOwnerOrAdmin ? (
        <div style={{ display: 'flex', alignItems: 'center', gap: 16, flexWrap: 'wrap' }}>
          <button className="btn" onClick={handleSyncNow} disabled={isSyncing}>
            {isSyncing ? 'Syncing…' : 'Sync now'}
          </button>
          <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 12.5 }}>
            <input
              type="checkbox"
              checked={status.autoSyncEnabled}
              disabled={isTogglingSync}
              onChange={(e) => handleToggleAutoSync(e.target.checked)}
            />
            Auto-sync every few hours
          </label>
        </div>
      ) : (
        <p style={{ fontSize: 11.5, color: 'var(--color-ink-faint)' }}>
          Only a team owner or admin can trigger a sync or change auto-sync.
        </p>
      )}
    </div>
  )
}
