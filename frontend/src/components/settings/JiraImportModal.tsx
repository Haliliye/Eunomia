import { useEffect, useState } from 'react'
import { integrationsApi, type JiraProject } from '@/api/integrations'
import type { ImportRow, ImportSummary } from '@/api/userStories'
import { useAuth } from '@/context/AuthContext'
import { teamsApi } from '@/api/teams'
import type { Team } from '@/types/team'
import { useToast } from '@/context/ToastContext'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface JiraImportModalProps {
  onClose: () => void
}

type Step = 'project' | 'team' | 'preview' | 'summary'

export default function JiraImportModal({ onClose }: JiraImportModalProps) {
  const { user } = useAuth()
  const { showToast } = useToast()
  useEscapeToClose(true, onClose)
  const containerRef = useFocusTrap(true)

  const [step, setStep] = useState<Step>('project')
  const [isBusy, setBusy] = useState(false)
  const [projects, setProjects] = useState<JiraProject[]>([])
  const [selectedProject, setSelectedProject] = useState<JiraProject | null>(null)
  const [myTeams, setMyTeams] = useState<Team[]>([])
  const [selectedTeamId, setSelectedTeamId] = useState('')
  const [preview, setPreview] = useState<ImportRow[] | null>(null)
  const [summary, setSummary] = useState<ImportSummary | null>(null)
  const [autoSync, setAutoSync] = useState(false)

  useEffect(() => {
    setBusy(true)
    integrationsApi.getJiraProjects()
      .then(setProjects)
      .catch(() => showToast("Couldn't load Jira projects.", 'error'))
      .finally(() => setBusy(false))
    // Only owner/admin teams can receive an import — same permission the backend enforces.
    teamsApi.getMyTeams(1, 100)
      .then((result) => setMyTeams(result.items.filter((t) =>
        t.members.some((m) => m.userId === user?.userId && (m.role === 'Owner' || m.role === 'Admin')))))
      .catch(() => {})
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handlePickProject = (project: JiraProject) => {
    setSelectedProject(project)
    setStep('team')
  }

  const handlePickTeam = async () => {
    if (!selectedProject || !selectedTeamId) return
    setBusy(true)
    try {
      const rows = await integrationsApi.previewJiraImport(selectedProject.key)
      setPreview(rows)
      setStep('preview')
    } catch {
      showToast("Couldn't load a preview for this project.", 'error')
    } finally {
      setBusy(false)
    }
  }

  const handleConfirm = async () => {
    if (!selectedProject || !selectedTeamId) return
    setBusy(true)
    try {
      const result = await integrationsApi.importJiraProject(selectedProject.key, selectedTeamId, autoSync)
      setSummary(result)
      setStep('summary')
    } catch {
      showToast("Import failed.", 'error')
    } finally {
      setBusy(false)
    }
  }

  const validCount = preview?.filter((r) => r.isValid).length ?? 0
  const invalidCount = (preview?.length ?? 0) - validCount

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" ref={containerRef} role="dialog" aria-modal="true" style={{ maxWidth: 640 }} onClick={(e) => e.stopPropagation()}>
        <h2>Import from Jira</h2>

        {step === 'project' && (
          <>
            <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 12 }}>
              Pick a Jira project to import its issues from.
            </p>
            {isBusy && <p>Loading projects…</p>}
            {!isBusy && projects.length === 0 && <p>No projects found on this Jira site.</p>}
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
          </>
        )}

        {step === 'team' && selectedProject && (
          <>
            <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 12 }}>
              Import <strong>{selectedProject.name}</strong> ({selectedProject.key}) into which team's backlog?
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
              Issues, labels, story points, comments, attachments, sprints, and issue links come
              along. Assignees are matched by email when they already have a Eunomia account —
              otherwise they get an email invite to join. Re-importing later updates existing
              stories instead of duplicating them.
            </p>
            <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 12, fontSize: 12.5 }}>
              <input type="checkbox" checked={autoSync} onChange={(e) => setAutoSync(e.target.checked)} />
              Keep this team in sync with Jira (re-imports automatically every few hours)
            </label>
          </>
        )}

        {step === 'preview' && preview && !isBusy && (
          <>
            <p style={{ fontSize: 13, margin: '12px 0' }}>
              <strong>{validCount}</strong> issue(s) will be imported.
              {invalidCount > 0 && <span style={{ color: 'var(--color-danger)' }}> {invalidCount} row(s) will be skipped.</span>}
            </p>
            <div style={{ maxHeight: 260, overflowY: 'auto', border: '1px solid var(--color-border)', borderRadius: 8 }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
                <thead>
                  <tr style={{ textAlign: 'left', borderBottom: '1px solid var(--color-border)' }}>
                    <th style={{ padding: 6 }}>Row</th>
                    <th style={{ padding: 6 }}>Title</th>
                    <th style={{ padding: 6 }}>Status</th>
                    <th style={{ padding: 6 }}>Result</th>
                  </tr>
                </thead>
                <tbody>
                  {preview.map((row) => (
                    <tr key={row.rowNumber} style={{ borderBottom: '1px solid var(--color-border)' }}>
                      <td style={{ padding: 6 }} className="mono">{row.rowNumber}</td>
                      <td style={{ padding: 6 }}>{row.title ?? '—'}</td>
                      <td style={{ padding: 6 }}>{row.status}</td>
                      <td style={{ padding: 6, color: row.isValid ? 'var(--color-done)' : 'var(--color-danger)' }}>
                        {row.isValid ? 'Will import' : row.error}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
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
            <button className="btn btn-primary" disabled={isBusy || !selectedTeamId} onClick={handlePickTeam}>
              Next: Preview
            </button>
          )}
          {step === 'preview' && (
            <button className="btn btn-primary" disabled={isBusy || validCount === 0} onClick={handleConfirm}>
              Import {validCount} stories
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
