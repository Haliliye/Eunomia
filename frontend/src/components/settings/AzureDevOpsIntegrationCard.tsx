import { useEffect, useState } from 'react'
import { azureDevOpsApi, type AzureDevOpsStatus } from '@/api/azureDevOps'
import { useToast } from '@/context/ToastContext'
import AzureDevOpsImportModal from './AzureDevOpsImportModal'

export default function AzureDevOpsIntegrationCard() {
  const { showToast } = useToast()
  const [status, setStatus] = useState<AzureDevOpsStatus | null>(null)
  const [isLoading, setLoading] = useState(true)
  const [isConnecting, setConnecting] = useState(false)
  const [showImportModal, setShowImportModal] = useState(false)
  const [organizations, setOrganizations] = useState<string[] | null>(null)
  const [selectedOrg, setSelectedOrg] = useState('')

  const loadStatus = () => azureDevOpsApi.getStatus().then(setStatus).finally(() => setLoading(false))

  useEffect(() => {
    // The OAuth callback redirects back here with ?azuredevops=connected or
    // ?azuredevops=error&message=... — surface that as a toast once, then
    // strip the params so a page refresh doesn't re-show it.
    const params = new URLSearchParams(window.location.search)
    const result = params.get('azuredevops')
    if (result === 'connected') {
      showToast('Azure DevOps connected.')
    } else if (result === 'error') {
      showToast(params.get('message') ?? 'Could not connect to Azure DevOps.', 'error')
    }
    if (result) {
      params.delete('azuredevops')
      params.delete('message')
      const newSearch = params.toString()
      window.history.replaceState(null, '', window.location.pathname + (newSearch ? `?${newSearch}` : ''))
    }

    loadStatus()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    // Connected but no organization picked yet (or picking a different one) —
    // fetch the list so the person can choose.
    if (status?.isConnected && organizations === null) {
      azureDevOpsApi.getOrganizations().then(setOrganizations).catch(() => setOrganizations([]))
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status?.isConnected])

  const handleConnect = async () => {
    setConnecting(true)
    try {
      const authorizationUrl = await azureDevOpsApi.connect()
      window.location.href = authorizationUrl // full-page redirect — Microsoft's consent screen can't run in an XHR
    } catch {
      showToast("Couldn't start the Azure DevOps connection.", 'error')
      setConnecting(false)
    }
  }

  const handleDisconnect = async () => {
    if (!window.confirm('Disconnect Azure DevOps? You can reconnect any time.')) return
    try {
      await azureDevOpsApi.disconnect()
      showToast('Azure DevOps disconnected.')
      setOrganizations(null)
      loadStatus()
    } catch {
      showToast("Couldn't disconnect Azure DevOps.", 'error')
    }
  }

  const handlePickOrganization = async (organizationName: string) => {
    try {
      await azureDevOpsApi.setOrganization(organizationName)
      loadStatus()
    } catch {
      showToast("Couldn't select that organization.", 'error')
    }
  }

  return (
    <div className="card" style={{ marginTop: 20 }}>
      <div className="card-header"><h3>Azure DevOps</h3></div>
      <p style={{ marginBottom: 12, fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
        Connect your Azure DevOps account to import a project's work items straight into a team's backlog.
      </p>

      {isLoading ? (
        <p>Loading…</p>
      ) : status?.isConnected ? (
        status.organizationName ? (
          <>
            <p style={{ marginBottom: 12 }}>
              Connected to <strong>{status.organizationName}</strong>.
            </p>
            <div style={{ display: 'flex', gap: 8 }}>
              <button className="btn btn-primary" onClick={() => setShowImportModal(true)}>Import a project…</button>
              <button className="btn" onClick={handleDisconnect}>Disconnect</button>
            </div>
          </>
        ) : (
          <>
            <p style={{ marginBottom: 8, fontSize: 12.5 }}>Which organization should Eunomia use?</p>
            {organizations === null ? (
              <p>Loading organizations…</p>
            ) : organizations.length === 0 ? (
              <p style={{ fontSize: 12.5, color: 'var(--color-danger)' }}>
                No Azure DevOps organizations found for this account.
              </p>
            ) : (
              <select className="input" value={selectedOrg} onChange={(e) => { setSelectedOrg(e.target.value); if (e.target.value) handlePickOrganization(e.target.value) }}>
                <option value="">Select an organization…</option>
                {organizations.map((org) => <option key={org} value={org}>{org}</option>)}
              </select>
            )}
          </>
        )
      ) : (
        <button className="btn btn-primary" onClick={handleConnect} disabled={isConnecting}>
          {isConnecting ? 'Redirecting…' : 'Connect Azure DevOps'}
        </button>
      )}

      {showImportModal && <AzureDevOpsImportModal onClose={() => setShowImportModal(false)} />}
    </div>
  )
}
