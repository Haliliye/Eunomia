import { useEffect, useState } from 'react'
import { azureDevOpsApi, type AzureDevOpsStatus } from '@/api/azureDevOps'
import { useToast } from '@/context/ToastContext'
import { useConfirm } from '@/context/ConfirmContext'

/**
 * One row inside ConnectionsCard. Unlike the other three (OAuth redirect),
 * Azure DevOps connects via a pasted Personal Access Token — this row
 * expands in place to show that form instead of redirecting anywhere.
 * Importing a project now happens from the "Import a team" flow on the
 * Teams page instead of from here, so this is connect/disconnect only.
 */
export default function AzureDevOpsIntegrationCard() {
  const { showToast } = useToast()
  const confirm = useConfirm()
  const [status, setStatus] = useState<AzureDevOpsStatus | null>(null)
  const [isLoading, setLoading] = useState(true)
  const [isConnecting, setConnecting] = useState(false)
  const [showConnectForm, setShowConnectForm] = useState(false)
  const [organizationName, setOrganizationName] = useState('')
  const [pat, setPat] = useState('')
  const [connectError, setConnectError] = useState<string | null>(null)

  const loadStatus = () => azureDevOpsApi.getStatus().then(setStatus).finally(() => setLoading(false))

  useEffect(() => { loadStatus() }, [])

  const handleConnect = async () => {
    if (!organizationName.trim() || !pat.trim()) return
    setConnecting(true)
    setConnectError(null)
    try {
      const result = await azureDevOpsApi.connect(organizationName.trim(), pat.trim())
      if (result.success) {
        showToast('Azure DevOps connected.')
        setPat('')
        setShowConnectForm(false)
        loadStatus()
      } else {
        setConnectError(result.errorMessage ?? "Couldn't connect.")
      }
    } catch (err: any) {
      setConnectError(err?.response?.data?.errorMessage ?? "Couldn't connect to Azure DevOps.")
    } finally {
      setConnecting(false)
    }
  }

  const handleDisconnect = async () => {
    const ok = await confirm({ title: 'Disconnect Azure DevOps?', description: 'You can reconnect any time.' })
    if (!ok) return
    try {
      await azureDevOpsApi.disconnect()
      showToast('Azure DevOps disconnected.')
      loadStatus()
    } catch {
      showToast("Couldn't disconnect Azure DevOps.", 'error')
    }
  }

  return (
    <div className="connection-row" style={{ flexDirection: 'column', alignItems: 'stretch' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <strong>Azure DevOps</strong>
          <p style={{ margin: '2px 0 0', fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
            {isLoading ? 'Loading…' : status?.isConnected ? <>Connected to <strong>{status.organizationName}</strong></> : 'Not connected'}
          </p>
        </div>
        {!isLoading && (
          status?.isConnected
            ? <button className="btn" onClick={handleDisconnect}>Disconnect</button>
            : <button className="btn btn-primary" onClick={() => setShowConnectForm((v) => !v)}>{showConnectForm ? 'Cancel' : 'Connect'}</button>
        )}
      </div>

      {showConnectForm && !status?.isConnected && (
        <div style={{ marginTop: 12 }}>
          <p style={{ marginBottom: 12, fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
            Connect with a Personal Access Token — go to your Azure DevOps organization, click your
            profile picture → <strong>Personal access tokens</strong> → <strong>+ New Token</strong>, and grant
            it <strong>Work Items (Read)</strong> and <strong>Project and Team (Read)</strong> scopes.
          </p>
          <div className="field">
            <label htmlFor="ado-org-name">Organization name</label>
            <input
              id="ado-org-name"
              className="input"
              placeholder="e.g. contoso (from dev.azure.com/contoso)"
              value={organizationName}
              onChange={(e) => setOrganizationName(e.target.value)}
            />
          </div>
          <div className="field">
            <label htmlFor="ado-pat">Personal access token</label>
            <input
              id="ado-pat"
              className="input"
              type="password"
              placeholder="Paste your token here"
              value={pat}
              onChange={(e) => setPat(e.target.value)}
            />
          </div>
          {connectError && <p className="field-error" role="alert">{connectError}</p>}
          <button className="btn btn-primary" onClick={handleConnect} disabled={isConnecting || !organizationName.trim() || !pat.trim()} style={{ marginTop: 8 }}>
            {isConnecting ? 'Connecting…' : 'Connect Azure DevOps'}
          </button>
        </div>
      )}
    </div>
  )
}
