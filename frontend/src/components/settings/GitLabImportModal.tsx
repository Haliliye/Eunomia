import { useEffect, useState } from 'react'
import { gitLabApi, type GitLabProject } from '@/api/gitlab'
import type { ImportSummary } from '@/api/userStories'
import { useAuth } from '@/context/AuthContext'
import { teamsApi } from '@/api/teams'
import type { Team } from '@/types/team'
import { useToast } from '@/context/ToastContext'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface GitLabImportModalProps {
  onClose: () => void
}

type Step = 'project' | 'team' | 'summary'

export default function GitLabImportModal({ onClose }: GitLabImportModalProps) {
  const { user } = useAuth()
  const { showToast } = useToast()
  useEscapeToClose(true, onClose)
  const containerRef = useFocusTrap(true)

  const [step, setStep] = useState<Step>('project')
  const [isBusy, setBusy] = useState(false)
  const [projects, setProjects] = useState<GitLabProject[]>([])
  const [selectedProject, setSelectedProject] = useState<GitLabProject | null>(null)
  const [myTeams, setMyTeams] = useState<Team[]>([])
  const [selectedTeamId, setSelectedTeamId] = useState('')
  const [summary, setSummary] = useState<ImportSummary | null>(null)

  useEffect(() => {
    setBusy(true)
    gitLabApi.getProjects()
      .then(setProjects)
      .catch(() => showToast("Couldn't load GitLab projects.", 'error'))
      .finally(() => setBusy(false))
    // Only owner/admin teams can receive an import — same permission the backend enforces.
    teamsApi.getMyTeams(1, 100)
      .then((result) => setMyTeams(result.items.filter((t) =>
        t.members.some((m) => m.userId === user?.userId && (m.role === 'Owner' || m.role === 'Admin')))))
      .catch(() => {})
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handlePickProject = (project: GitLabProject) => {
    setSelectedProject(project)
    setStep('team')
  }

  const handleConfirm = async () => {
    if (!selectedProject || !selectedTeamId) return
    setBusy(true)
    try {
      const result = await gitLabApi.importProject(selectedProject.id, selectedProject.pathWithNamespace, selectedTeamId)
      setSummary(result)
      setStep('summary')
    } catch {
      showToast('Import failed.', 'error')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" ref={containerRef} role="dialog" aria-modal="true" style={{ maxWidth: 640 }} onClick={(e) => e.stopPropagation()}>
        <h2>Import from GitLab</h2>

        {step === 'project' && (
          <>
            <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 12 }}>
              Pick a project to import its issues from.
            </p>
            {isBusy && <p>Loading projects…</p>}
            {!isBusy && projects.length === 0 && <p>No projects found for this GitLab account.</p>}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6, maxHeight: 320, overflowY: 'auto' }}>
              {projects.map((p) => (
                <button
                  key={p.id}
                  className="btn"
                  style={{ justifyContent: 'flex-start', textAlign: 'left' }}
                  onClick={() => handlePickProject(p)}
                >
                  {p.pathWithNamespace}
                </button>
              ))}
            </div>
          </>
        )}

        {step === 'team' && selectedProject && (
          <>
            <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 12 }}>
              Import <strong>{selectedProject.pathWithNamespace}</strong> into which team's backlog?
            </p>
            {myTeams.length === 0 ? (
              <p style={{ fontSize: 12.5, color: 'var(--color-danger)' }}>
                You need to be an owner or admin of at least one team to import into it.
              </p>
            ) : (
              <select className="input" value={selectedTeamId} onChange={(e) => setSelectedTeamId(e.target.value)}>
                <option value="">Select a team…</option>
                {myTeams.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
              </select>
            )}
            <p style={{ fontSize: 11.5, color: 'var(--color-ink-faint)', marginTop: 8 }}>
              Open issues, labels, and notes (comments) come along — closed issues land in Done,
              everything else in To Do. Assignees are matched by email when they've made it public
              on their GitLab profile and already have a Eunomia account — otherwise the issue comes
              in unassigned. Re-importing later updates existing stories instead of duplicating them.
            </p>
          </>
        )}

        {step === 'summary' && summary && (
          <p style={{ marginTop: 12 }}>
            <strong>{summary.createdCount}</strong> stories created.
            {summary.updatedCount > 0 && ` ${summary.updatedCount} existing stories updated.`}
            {summary.skippedCount > 0 && ` ${summary.skippedCount} row(s) skipped.`}
          </p>
        )}

        {isBusy && step !== 'project' && <p style={{ marginTop: 12 }}>Working…</p>}

        <div className="modal-actions" style={{ marginTop: 16 }}>
          <button className="btn" onClick={onClose}>{step === 'summary' ? 'Close' : 'Cancel'}</button>
          {step === 'team' && (
            <button className="btn btn-primary" disabled={isBusy || !selectedTeamId} onClick={handleConfirm}>
              Import
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
