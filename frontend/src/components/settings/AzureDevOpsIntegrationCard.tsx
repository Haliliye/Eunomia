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
    if (!window.confirm('Disconnect Azure DevOps? You can reconnect any time.')) return
    try {
      await azureDevOpsApi.disconnect()
      showToast('Azure DevOps disconnected.')
      loadStatus()
    } catch {
      showToast("Couldn't disconnect Azure DevOps.", 'error')
    }
  }

  return (
    <div className="card" style={{ marginTop: 20 }}>
      <div className="card-header"><h3>Azure DevOps</h3></div>

      {isLoading ? (
        <p>Loading…</p>
      ) : status?.isConnected ? (
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
        </>
      )}

      {showImportModal && <AzureDevOpsImportModal onClose={() => setShowImportModal(false)} />}
    </div>
  )
}
