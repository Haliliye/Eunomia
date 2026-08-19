import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { gitHubApi, type GitHubRepository, type CreateTeamFromGitHubResult } from '@/api/github'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface CreateTeamFromGitHubModalProps {
  onClose: () => void
}

type Step = 'checking' | 'notConnected' | 'repo' | 'name' | 'creating' | 'summary'

export default function CreateTeamFromGitHubModal({ onClose }: CreateTeamFromGitHubModalProps) {
  const navigate = useNavigate()
  useEscapeToClose(true, onClose)
  const containerRef = useFocusTrap(true)

  const [step, setStep] = useState<Step>('checking')
  const [repositories, setRepositories] = useState<GitHubRepository[]>([])
  const [selectedRepo, setSelectedRepo] = useState<GitHubRepository | null>(null)
  const [teamName, setTeamName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<CreateTeamFromGitHubResult | null>(null)

  useEffect(() => {
    gitHubApi.getStatus()
      .then((status) => {
        if (!status.isConnected) {
          setStep('notConnected')
          return
        }
        return gitHubApi.getRepositories().then((list) => {
          setRepositories(list)
          setStep('repo')
        })
      })
      .catch(() => {
        setStep('notConnected')
      })
  }, [])

  const handlePickRepo = (repo: GitHubRepository) => {
    setSelectedRepo(repo)
    setTeamName(repo.name)
    setStep('name')
  }

  const handleCreate = async () => {
    if (!selectedRepo || !teamName.trim()) return
    setStep('creating')
    setError(null)
    try {
      const created = await gitHubApi.createTeamFromRepo(selectedRepo.owner, selectedRepo.name, teamName.trim())
      setResult(created)
      setStep('summary')
    } catch (err: any) {
      setError(err?.response?.data?.error ?? "Couldn't create the team from this repo.")
      setStep('name')
    }
  }

  const handleGoToTeam = () => {
    if (!result) return
    onClose()
    navigate(`/teams/${result.team.id}`)
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" ref={containerRef} role="dialog" aria-modal="true" style={{ maxWidth: 560 }} onClick={(e) => e.stopPropagation()}>
        <h2>New team from GitHub</h2>

        {step === 'checking' && <p>Checking your GitHub connection…</p>}

        {step === 'notConnected' && (
          <>
            <p style={{ fontSize: 13, marginBottom: 12 }}>
              You need to connect GitHub before you can create a team from a repo.
            </p>
            <div className="modal-actions">
              <button className="btn" onClick={onClose}>Cancel</button>
              <button className="btn btn-primary" onClick={() => { onClose(); navigate('/settings') }}>Go to Settings</button>
            </div>
          </>
        )}

        {step === 'repo' && (
          <>
            <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 12 }}>
              Pick a repository — a new team will be created from it, with its open issues, labels, and comments.
            </p>
            {repositories.length === 0 && <p>No repositories found for this GitHub account.</p>}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6, maxHeight: 320, overflowY: 'auto' }}>
              {repositories.map((r) => (
                <button
                  key={r.fullName}
                  className="btn"
                  style={{ justifyContent: 'flex-start', textAlign: 'left' }}
                  onClick={() => handlePickRepo(r)}
                >
                  {r.fullName}
                </button>
              ))}
            </div>
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={onClose}>Cancel</button>
            </div>
          </>
        )}

        {step === 'name' && selectedRepo && (
          <>
            <div className="field">
              <label htmlFor="github-team-name">Team name</label>
              <input id="github-team-name" className="input" value={teamName} onChange={(e) => setTeamName(e.target.value)} maxLength={50} autoFocus />
            </div>
            <p style={{ fontSize: 11.5, color: 'var(--color-ink-faint)', marginTop: 4 }}>
              Open issues in {selectedRepo.fullName} will be imported, along with labels and comments —
              closed issues land in Done, everything else in To Do. Assignees with a public GitHub
              email and a matching Eunomia account are assigned automatically; pull requests aren't imported.
            </p>
            {error && <p className="field-error" role="alert">{error}</p>}
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={() => setStep('repo')}>Back</button>
              <button className="btn btn-primary" disabled={!teamName.trim()} onClick={handleCreate}>Create team</button>
            </div>
          </>
        )}

        {step === 'creating' && <p>Creating your team and importing issues…</p>}

        {step === 'summary' && result && (
          <>
            <p style={{ marginTop: 4 }}>
              <strong>"{result.team.name}"</strong> was created with <strong>{result.importSummary.createdCount}</strong> stories.
              {result.importSummary.updatedCount > 0 && ` ${result.importSummary.updatedCount} existing stories updated.`}
              {result.importSummary.skippedCount > 0 && ` ${result.importSummary.skippedCount} row(s) were skipped.`}
            </p>
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={onClose}>Close</button>
              <button className="btn btn-primary" onClick={handleGoToTeam}>Open team</button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
