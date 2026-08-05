import { useEffect, useState } from 'react'
import { integrationsApi, type JiraStatus } from '@/api/integrations'
import { useToast } from '@/context/ToastContext'
import JiraImportModal from './JiraImportModal'

export default function JiraIntegrationCard() {
  const { showToast } = useToast()
  const [status, setStatus] = useState<JiraStatus | null>(null)
  const [isLoading, setLoading] = useState(true)
  const [isConnecting, setConnecting] = useState(false)
  const [showImportModal, setShowImportModal] = useState(false)

  const loadStatus = () => integrationsApi.getJiraStatus().then(setStatus).finally(() => setLoading(false))

  useEffect(() => {
    // The OAuth callback redirects back here with ?jira=connected or
    // ?jira=error&message=... — surface that as a toast once, then strip
    // the params so a page refresh doesn't re-show it.
    const params = new URLSearchParams(window.location.search)
    const jiraResult = params.get('jira')
    if (jiraResult === 'connected') {
      showToast('Jira connected.')
    } else if (jiraResult === 'error') {
      showToast(params.get('message') ?? 'Could not connect to Jira.', 'error')
    }
    if (jiraResult) {
      params.delete('jira')
      params.delete('message')
      const newSearch = params.toString()
      window.history.replaceState(null, '', window.location.pathname + (newSearch ? `?${newSearch}` : ''))
    }

    loadStatus()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleConnect = async () => {
    setConnecting(true)
    try {
      const authorizationUrl = await integrationsApi.connectJira()
      window.location.href = authorizationUrl // full-page redirect — Atlassian's consent screen can't run in an XHR
    } catch {
      showToast("Couldn't start the Jira connection.", 'error')
      setConnecting(false)
    }
  }

  const handleDisconnect = async () => {
    if (!window.confirm('Disconnect Jira? You can reconnect any time.')) return
    try {
      await integrationsApi.disconnectJira()
      showToast('Jira disconnected.')
      loadStatus()
    } catch {
      showToast("Couldn't disconnect Jira.", 'error')
    }
  }

  return (
    <div className="card" style={{ marginTop: 20 }}>
      <div className="card-header"><h3>Jira</h3></div>
      <p style={{ marginBottom: 12, fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
        Connect your Jira account to import a project's issues straight into a team's backlog.
      </p>

      {isLoading ? (
        <p>Loading…</p>
      ) : status?.isConnected ? (
        <>
          <p style={{ marginBottom: 12 }}>
            Connected to <strong>{status.siteName}</strong>.
          </p>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn btn-primary" onClick={() => setShowImportModal(true)}>Import a project…</button>
            <button className="btn" onClick={handleDisconnect}>Disconnect</button>
          </div>
        </>
      ) : (
        <button className="btn btn-primary" onClick={handleConnect} disabled={isConnecting}>
          {isConnecting ? 'Redirecting…' : 'Connect Jira'}
        </button>
      )}

      {showImportModal && <JiraImportModal onClose={() => setShowImportModal(false)} />}
    </div>
  )
}
