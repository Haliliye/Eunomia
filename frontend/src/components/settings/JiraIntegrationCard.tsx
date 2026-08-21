import { useEffect, useState } from 'react'
import { integrationsApi, type JiraStatus } from '@/api/integrations'
import { useToast } from '@/context/ToastContext'
import { useConfirm } from '@/context/ConfirmContext'

/** One row inside ConnectionsCard — importing a project now happens from the "Import a team" flow on the Teams page instead of from here, so this is connect/disconnect only. */
export default function JiraIntegrationCard() {
  const { showToast } = useToast()
  const confirm = useConfirm()
  const [status, setStatus] = useState<JiraStatus | null>(null)
  const [isLoading, setLoading] = useState(true)
  const [isConnecting, setConnecting] = useState(false)

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
    const ok = await confirm({ title: 'Disconnect Jira?', description: 'You can reconnect any time.' })
    if (!ok) return
    try {
      await integrationsApi.disconnectJira()
      showToast('Jira disconnected.')
      loadStatus()
    } catch {
      showToast("Couldn't disconnect Jira.", 'error')
    }
  }

  return (
    <div className="connection-row">
      <div>
        <strong>Jira</strong>
        <p style={{ margin: '2px 0 0', fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
          {isLoading ? 'Loading…' : status?.isConnected ? <>Connected to <strong>{status.siteName}</strong></> : 'Not connected'}
        </p>
      </div>
      {!isLoading && (
        status?.isConnected
          ? <button className="btn" onClick={handleDisconnect}>Disconnect</button>
          : <button className="btn btn-primary" onClick={handleConnect} disabled={isConnecting}>{isConnecting ? 'Redirecting…' : 'Connect'}</button>
      )}
    </div>
  )
}
