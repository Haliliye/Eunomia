import { useEffect, useState } from 'react'
import { gitLabApi, type GitLabStatus } from '@/api/gitlab'
import { useToast } from '@/context/ToastContext'
import { useConfirm } from '@/context/ConfirmContext'
import GitLabImportModal from './GitLabImportModal'

export default function GitLabIntegrationCard() {
  const { showToast } = useToast()
  const confirm = useConfirm()
  const [status, setStatus] = useState<GitLabStatus | null>(null)
  const [isLoading, setLoading] = useState(true)
  const [isConnecting, setConnecting] = useState(false)
  const [showImportModal, setShowImportModal] = useState(false)

  const loadStatus = () => gitLabApi.getStatus().then(setStatus).finally(() => setLoading(false))

  useEffect(() => {
    // The OAuth callback redirects back here with ?gitlab=connected or
    // ?gitlab=error&message=... — surface that as a toast once, then strip
    // the params so a page refresh doesn't re-show it.
    const params = new URLSearchParams(window.location.search)
    const result = params.get('gitlab')
    if (result === 'connected') {
      showToast('GitLab connected.')
    } else if (result === 'error') {
      showToast(params.get('message') ?? 'Could not connect to GitLab.', 'error')
    }
    if (result) {
      params.delete('gitlab')
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
      const authorizationUrl = await gitLabApi.connect()
      window.location.href = authorizationUrl // full-page redirect — GitLab's consent screen can't run in an XHR
    } catch {
      showToast("Couldn't start the GitLab connection.", 'error')
      setConnecting(false)
    }
  }

  const handleDisconnect = async () => {
    const ok = await confirm({ title: 'Disconnect GitLab?', description: 'You can reconnect any time.' })
    if (!ok) return
    try {
      await gitLabApi.disconnect()
      showToast('GitLab disconnected.')
      loadStatus()
    } catch {
      showToast("Couldn't disconnect GitLab.", 'error')
    }
  }

  return (
    <div className="card" style={{ marginTop: 20 }}>
      <div className="card-header"><h3>GitLab</h3></div>
      <p style={{ marginBottom: 12, fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
        Connect your GitLab account to import a project's issues straight into a team's backlog.
      </p>

      {isLoading ? (
        <p>Loading…</p>
      ) : status?.isConnected ? (
        <>
          <p style={{ marginBottom: 12 }}>
            Connected as <strong>@{status.gitLabUsername}</strong>.
          </p>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn btn-primary" onClick={() => setShowImportModal(true)}>Import a project…</button>
            <button className="btn" onClick={handleDisconnect}>Disconnect</button>
          </div>
        </>
      ) : (
        <button className="btn btn-primary" onClick={handleConnect} disabled={isConnecting}>
          {isConnecting ? 'Redirecting…' : 'Connect GitLab'}
        </button>
      )}

      {showImportModal && <GitLabImportModal onClose={() => setShowImportModal(false)} />}
    </div>
  )
}
