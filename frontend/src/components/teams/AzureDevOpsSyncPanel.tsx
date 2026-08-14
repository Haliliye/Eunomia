import { useEffect, useState } from 'react'
import { azureDevOpsApi, type AzureDevOpsSyncStatus } from '@/api/azureDevOps'
import type { Team } from '@/types/team'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/context/ToastContext'

interface AzureDevOpsSyncPanelProps {
  team: Team
}

export default function AzureDevOpsSyncPanel({ team }: AzureDevOpsSyncPanelProps) {
  const { user } = useAuth()
  const { showToast } = useToast()
  const [status, setStatus] = useState<AzureDevOpsSyncStatus | null>(null)
  const [isSyncing, setSyncing] = useState(false)
  const [isTogglingSync, setTogglingSync] = useState(false)
  const [showHistory, setShowHistory] = useState(false)

  const isOwnerOrAdmin = team.members.some((m) => m.userId === user?.userId && (m.role === 'Owner' || m.role === 'Admin'))

  const load = () => azureDevOpsApi.getSyncStatus(team.id).then(setStatus).catch(() => setStatus(null))

  useEffect(() => { load() }, [team.id])

  if (!status?.isLinked) return null // not every team came from Azure DevOps — nothing to show for the rest

  const handleSyncNow = async () => {
    setSyncing(true)
    try {
      const summary = await azureDevOpsApi.syncTeamNow(team.id)
      showToast(`Synced: ${summary.createdCount} created, ${summary.updatedCount} updated.`)
      load()
    } catch {
      showToast("Sync failed. Your Azure DevOps connection may have expired — try reconnecting from Settings.", 'error')
    } finally {
      setSyncing(false)
    }
  }

  const handleToggleAutoSync = async (enabled: boolean) => {
    setTogglingSync(true)
    try {
      await azureDevOpsApi.setAutoSync(team.id, enabled)
      load()
    } catch {
      showToast("Couldn't update the auto-sync setting.", 'error')
    } finally {
      setTogglingSync(false)
    }
  }

  return (
    <div className="card" style={{ marginBottom: 20 }}>
      <div className="card-header"><h3>Azure DevOps</h3></div>
      <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 8 }}>
        Linked to Azure DevOps project <strong>{status.projectName}</strong>.{' '}
        {status.lastSyncedOn ? `Last synced ${new Date(status.lastSyncedOn).toLocaleString()}.` : 'Never synced yet.'}
        {status.history && status.history.length > 0 && (
          <button
            className="btn btn-ghost btn-sm"
            style={{ marginLeft: 6, padding: '0 4px', fontSize: 11.5 }}
            onClick={() => setShowHistory((v) => !v)}
          >
            {showHistory ? 'Hide history' : 'Show history'}
          </button>
        )}
      </p>

      {showHistory && status.history && status.history.length > 0 && (
        <ul style={{ fontSize: 12, listStyle: 'none', padding: 0, margin: '0 0 12px', borderLeft: '2px solid var(--color-border)' }}>
          {status.history.map((entry, i) => (
            <li key={i} style={{ padding: '4px 0 4px 10px' }}>
              <span className="mono" style={{ color: 'var(--color-ink-faint)' }}>{new Date(entry.syncedOn).toLocaleString()}</span>
              {' — '}
              {entry.createdCount} created, {entry.updatedCount} updated
              {entry.skippedCount > 0 && `, ${entry.skippedCount} skipped`}
            </li>
          ))}
        </ul>
      )}

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
