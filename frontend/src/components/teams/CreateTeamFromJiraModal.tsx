import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { integrationsApi, type JiraProject, type CreateTeamFromJiraResult } from '@/api/integrations'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface CreateTeamFromJiraModalProps {
  onClose: () => void
}

type Step = 'checking' | 'notConnected' | 'project' | 'name' | 'creating' | 'summary'

export default function CreateTeamFromJiraModal({ onClose }: CreateTeamFromJiraModalProps) {
  const navigate = useNavigate()
  useEscapeToClose(true, onClose)
  const containerRef = useFocusTrap(true)

  const [step, setStep] = useState<Step>('checking')
  const [projects, setProjects] = useState<JiraProject[]>([])
  const [selectedProject, setSelectedProject] = useState<JiraProject | null>(null)
  const [teamName, setTeamName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<CreateTeamFromJiraResult | null>(null)

  useEffect(() => {
    integrationsApi.getJiraStatus()
      .then((status) => {
        if (!status.isConnected) {
          setStep('notConnected')
          return
        }
        return integrationsApi.getJiraProjects().then((list) => {
          setProjects(list)
          setStep('project')
        })
      })
      .catch(() => {
        setStep('notConnected')
      })
  }, [])

  const handlePickProject = (project: JiraProject) => {
    setSelectedProject(project)
    setTeamName(project.name)
    setStep('name')
  }

  const handleCreate = async () => {
    if (!selectedProject || !teamName.trim()) return
    setStep('creating')
    setError(null)
    try {
      const created = await integrationsApi.createTeamFromJira(selectedProject.key, teamName.trim())
      setResult(created)
      setStep('summary')
    } catch (err: any) {
      setError(err?.response?.data?.error ?? "Couldn't create the team from this project.")
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
        <h2>New team from Jira</h2>

        {step === 'checking' && <p>Checking your Jira connection…</p>}

        {step === 'notConnected' && (
          <>
            <p style={{ fontSize: 13, marginBottom: 12 }}>
              You need to connect Jira before you can create a team from a project.
            </p>
            <div className="modal-actions">
              <button className="btn" onClick={onClose}>Cancel</button>
              <button className="btn btn-primary" onClick={() => { onClose(); navigate('/settings') }}>Go to Settings</button>
            </div>
          </>
        )}

        {step === 'project' && (
          <>
            <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 12 }}>
              Pick a Jira project — a new team will be created from it, with its issues, labels, and assignees.
            </p>
            {projects.length === 0 && <p>No projects found on this Jira site.</p>}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6, maxHeight: 320, overflowY: 'auto' }}>
              {projects.map((p) => (
                <button
                  key={p.key}
                  className="btn"
                  style={{ justifyContent: 'flex-start', textAlign: 'left' }}
                  onClick={() => handlePickProject(p)}
                >
                  <strong style={{ marginRight: 8 }}>{p.key}</strong> {p.name}
                </button>
              ))}
            </div>
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={onClose}>Cancel</button>
            </div>
          </>
        )}

        {step === 'name' && selectedProject && (
          <>
            <div className="field">
              <label htmlFor="jira-team-name">Team name</label>
              <input id="jira-team-name" className="input" value={teamName} onChange={(e) => setTeamName(e.target.value)} maxLength={50} autoFocus />
            </div>
            <p style={{ fontSize: 11.5, color: 'var(--color-ink-faint)', marginTop: 4 }}>
              Every issue in {selectedProject.name} ({selectedProject.key}) will be imported. Assignees with a matching
              Eunomia account are assigned automatically; others get an email invitation to join and are added to this
              team once they sign up. Labels that don't exist yet are created for you.
            </p>
            {error && <p className="field-error" role="alert">{error}</p>}
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={() => setStep('project')}>Back</button>
              <button className="btn btn-primary" disabled={!teamName.trim()} onClick={handleCreate}>Create team</button>
            </div>
          </>
        )}

        {step === 'creating' && <p>Creating your team and importing issues…</p>}

        {step === 'summary' && result && (
          <>
            <p style={{ marginTop: 4 }}>
              <strong>"{result.team.name}"</strong> was created with <strong>{result.importSummary.createdCount}</strong> stories.
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
