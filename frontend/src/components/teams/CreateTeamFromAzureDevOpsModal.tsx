import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { azureDevOpsApi, type AzureDevOpsProject, type CreateTeamFromAzureDevOpsResult } from '@/api/azureDevOps'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface CreateTeamFromAzureDevOpsModalProps {
  onClose: () => void
}

type Step = 'checking' | 'notConnected' | 'project' | 'name' | 'creating' | 'summary'

export default function CreateTeamFromAzureDevOpsModal({ onClose }: CreateTeamFromAzureDevOpsModalProps) {
  const navigate = useNavigate()
  useEscapeToClose(true, onClose)
  const containerRef = useFocusTrap(true)

  const [step, setStep] = useState<Step>('checking')
  const [projects, setProjects] = useState<AzureDevOpsProject[]>([])
  const [selectedProject, setSelectedProject] = useState<AzureDevOpsProject | null>(null)
  const [teamName, setTeamName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [autoSync, setAutoSync] = useState(false)
  const [result, setResult] = useState<CreateTeamFromAzureDevOpsResult | null>(null)

  useEffect(() => {
    azureDevOpsApi.getStatus()
      .then((status) => {
        if (!status.isConnected) { setStep('notConnected'); return }
        return azureDevOpsApi.getProjects().then((list) => {
          setProjects(list)
          setStep('project')
        })
      })
      .catch(() => setStep('notConnected'))
  }, [])

  const handlePickProject = (project: AzureDevOpsProject) => {
    setSelectedProject(project)
    setTeamName(project.name)
    setStep('name')
  }

  const handleCreate = async () => {
    if (!selectedProject || !teamName.trim()) return
    setStep('creating')
    setError(null)
    try {
      const created = await azureDevOpsApi.createTeamFromProject(selectedProject.name, teamName.trim(), autoSync)
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
        <h2>New team from Azure DevOps</h2>

        {step === 'checking' && <p>Checking your Azure DevOps connection…</p>}

        {step === 'notConnected' && (
          <>
            <p style={{ fontSize: 13, marginBottom: 12 }}>
              You need to connect Azure DevOps before you can create a team from a project.
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
              Pick an Azure DevOps project — a new team will be created from it, with its work items, tags, and assignees.
            </p>
            {projects.length === 0 && <p>No projects found in this organization.</p>}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6, maxHeight: 320, overflowY: 'auto' }}>
              {projects.map((p) => (
                <button
                  key={p.id}
                  className="btn"
                  style={{ justifyContent: 'flex-start', textAlign: 'left' }}
                  onClick={() => handlePickProject(p)}
                >
                  {p.name}
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
              <label htmlFor="ado-team-name">Team name</label>
              <input id="ado-team-name" className="input" value={teamName} onChange={(e) => setTeamName(e.target.value)} maxLength={50} autoFocus />
            </div>
            <p style={{ fontSize: 11.5, color: 'var(--color-ink-faint)', marginTop: 4 }}>
              Every work item in {selectedProject.name} will be imported, along with tags, story
              points, comments, attachments, iterations, and work item links. The new team's board
              gets one column per distinct work item state. Assignees with a matching Eunomia
              account are assigned automatically; others get an email invitation to join and are
              added to this team once they sign up.
            </p>
            <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 8, fontSize: 12.5 }}>
              <input type="checkbox" checked={autoSync} onChange={(e) => setAutoSync(e.target.checked)} />
              Keep this team in sync with Azure DevOps (re-imports automatically every few hours)
            </label>
            {error && <p className="field-error" role="alert">{error}</p>}
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={() => setStep('project')}>Back</button>
              <button className="btn btn-primary" disabled={!teamName.trim()} onClick={handleCreate}>Create team</button>
            </div>
          </>
        )}

        {step === 'creating' && <p>Creating your team and importing work items…</p>}

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
