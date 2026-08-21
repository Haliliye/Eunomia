import { useEffect, useState } from 'react'
import { gitHubApi, type GitHubStatus } from '@/api/github'
import { useToast } from '@/context/ToastContext'
import { useConfirm } from '@/context/ConfirmContext'

/** One row inside ConnectionsCard — importing a repo now happens from the "Import a team" flow on the Teams page instead of from here, so this is connect/disconnect only. */
export default function GitHubIntegrationCard() {
  const { showToast } = useToast()
  const confirm = useConfirm()
  const [status, setStatus] = useState<GitHubStatus | null>(null)
  const [isLoading, setLoading] = useState(true)
  const [isConnecting, setConnecting] = useState(false)

  const loadStatus = () => gitHubApi.getStatus().then(setStatus).finally(() => setLoading(false))

  useEffect(() => {
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
      window.location.href = authorizationUrl
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
    <div className="connection-row">
      <div>
        <strong>GitHub</strong>
        <p style={{ margin: '2px 0 0', fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
          {isLoading ? 'Loading…' : status?.isConnected ? <>Connected as <strong>@{status.gitHubLogin}</strong></> : 'Not connected'}
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
