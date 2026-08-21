import { useEffect, useState } from 'react'
import { gitLabApi, type GitLabStatus } from '@/api/gitlab'
import { useToast } from '@/context/ToastContext'
import { useConfirm } from '@/context/ConfirmContext'

/** One row inside ConnectionsCard — importing a project now happens from the "Import a team" flow on the Teams page instead of from here, so this is connect/disconnect only. */
export default function GitLabIntegrationCard() {
  const { showToast } = useToast()
  const confirm = useConfirm()
  const [status, setStatus] = useState<GitLabStatus | null>(null)
  const [isLoading, setLoading] = useState(true)
  const [isConnecting, setConnecting] = useState(false)

  const loadStatus = () => gitLabApi.getStatus().then(setStatus).finally(() => setLoading(false))

  useEffect(() => {
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
      window.location.href = authorizationUrl
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
    <div className="connection-row">
      <div>
        <strong>GitLab</strong>
        <p style={{ margin: '2px 0 0', fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
          {isLoading ? 'Loading…' : status?.isConnected ? <>Connected as <strong>@{status.gitLabUsername}</strong></> : 'Not connected'}
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
