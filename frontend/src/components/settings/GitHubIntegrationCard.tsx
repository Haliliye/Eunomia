import { useEffect, useState } from 'react'
import { gitHubApi, type GitHubStatus } from '@/api/github'
import { useToast } from '@/context/ToastContext'
import { useConfirm } from '@/context/ConfirmContext'
import GitHubImportModal from './GitHubImportModal'

export default function GitHubIntegrationCard() {
  const { showToast } = useToast()
  const confirm = useConfirm()
  const [status, setStatus] = useState<GitHubStatus | null>(null)
  const [isLoading, setLoading] = useState(true)
  const [isConnecting, setConnecting] = useState(false)
  const [showImportModal, setShowImportModal] = useState(false)

  const loadStatus = () => gitHubApi.getStatus().then(setStatus).finally(() => setLoading(false))

  useEffect(() => {
    // The OAuth callback redirects back here with ?github=connected or
    // ?github=error&message=... — surface that as a toast once, then strip
    // the params so a page refresh doesn't re-show it.
    const params = new URLSearchParams(window.location.search)
    const result = params.get('github')
    if (result === 'connected') {
      showToast('GitHub connected.')
    } else if (result === 'error') {
      showToast(params.get('message') ?? 'Could not connect to GitHub.', 'error')
    }
    if (result) {
      params.delete('github')
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
      const authorizationUrl = await gitHubApi.connect()
      window.location.href = authorizationUrl // full-page redirect — GitHub's consent screen can't run in an XHR
    } catch {
      showToast("Couldn't start the GitHub connection.", 'error')
      setConnecting(false)
    }
  }

  const handleDisconnect = async () => {
    const ok = await confirm({ title: 'Disconnect GitHub?', description: 'You can reconnect any time.' })
    if (!ok) return
    try {
      await gitHubApi.disconnect()
      showToast('GitHub disconnected.')
      loadStatus()
    } catch {
      showToast("Couldn't disconnect GitHub.", 'error')
    }
  }

  return (
    <div className="card" style={{ marginTop: 20 }}>
      <div className="card-header"><h3>GitHub</h3></div>
      <p style={{ marginBottom: 12, fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
        Connect your GitHub account to import a repo's issues straight into a team's backlog.
      </p>

      {isLoading ? (
        <p>Loading…</p>
      ) : status?.isConnected ? (
        <>
          <p style={{ marginBottom: 12 }}>
            Connected as <strong>@{status.gitHubLogin}</strong>.
          </p>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn btn-primary" onClick={() => setShowImportModal(true)}>Import a repo…</button>
            <button className="btn" onClick={handleDisconnect}>Disconnect</button>
          </div>
        </>
      ) : (
        <button className="btn btn-primary" onClick={handleConnect} disabled={isConnecting}>
          {isConnecting ? 'Redirecting…' : 'Connect GitHub'}
        </button>
      )}

      {showImportModal && <GitHubImportModal onClose={() => setShowImportModal(false)} />}
    </div>
  )
}
