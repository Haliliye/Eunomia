import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { importProviders, type ImportProviderAdapter, type ImportableItem, type CreateTeamResult } from '@/lib/importProviders'
import type { ImportRow, ImportSummary } from '@/api/userStories'
import { useAuth } from '@/context/AuthContext'
import { teamsApi } from '@/api/teams'
import type { Team } from '@/types/team'
import { useToast } from '@/context/ToastContext'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface ImportTeamModalProps {
  onClose: () => void
}

type Destination = 'new' | 'existing'

type Step =
  | 'source' | 'checking' | 'notConnected' | 'item' | 'destination'
  | 'name' | 'creating' // new-team path
  | 'team' | 'preview' | 'importing' // existing-team path
  | 'summary'

/**
 * Replaces eight separate near-duplicate modals (four CreateTeamFrom*Modal,
 * four *ImportModal) with one flow: pick a source, pick a project/repo, then
 * choose whether to create a brand-new team from it or import it into a
 * team you already own/admin — driven off that provider's
 * ImportProviderAdapter for every API-shaped difference between the four.
 */
export default function ImportTeamModal({ onClose }: ImportTeamModalProps) {
  const navigate = useNavigate()
  const { user } = useAuth()
  const { showToast } = useToast()
  useEscapeToClose(true, onClose)
  const containerRef = useFocusTrap(true)

  const [step, setStep] = useState<Step>('source')
  const [provider, setProvider] = useState<ImportProviderAdapter | null>(null)
  const [items, setItems] = useState<ImportableItem[]>([])
  const [selectedItem, setSelectedItem] = useState<ImportableItem | null>(null)

  // New-team path
  const [teamName, setTeamName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [createResult, setCreateResult] = useState<CreateTeamResult | null>(null)

  // Existing-team path
  const [myTeams, setMyTeams] = useState<Team[]>([])
  const [selectedTeamId, setSelectedTeamId] = useState('')
  const [preview, setPreview] = useState<ImportRow[] | null>(null)
  const [importSummary, setImportSummary] = useState<ImportSummary | null>(null)

  // Shared
  const [autoSync, setAutoSync] = useState(false)

  useEffect(() => {
    if (!provider) return
    setStep('checking')
    provider.getIsConnected()
      .then((isConnected) => {
        if (!isConnected) { setStep('notConnected'); return }
        return provider.listItems().then((list) => {
          setItems(list)
          setStep('item')
        })
      })
      .catch(() => setStep('notConnected'))
  }, [provider])

  const handlePickProvider = (p: ImportProviderAdapter) => setProvider(p)

  const handlePickItem = (item: ImportableItem) => {
    setSelectedItem(item)
    setStep('destination')
  }

  const handlePickDestination = (dest: Destination) => {
    if (dest === 'new') {
      setTeamName(provider!.defaultTeamNameFor(selectedItem!))
      setStep('name')
    } else {
      // Only owner/admin teams can receive an import — same permission the backend enforces.
      teamsApi.getMyTeams(1, 100)
        .then((result) => setMyTeams(result.items.filter((t) =>
          t.members.some((m) => m.userId === user?.userId && (m.role === 'Owner' || m.role === 'Admin')))))
        .catch(() => {})
      setStep('team')
    }
  }

  const handleCreateNewTeam = async () => {
    if (!provider || !selectedItem || !teamName.trim()) return
    setStep('creating')
    setError(null)
    try {
      const result = await provider.createTeam(selectedItem, teamName.trim(), autoSync)
      setCreateResult(result)
      setStep('summary')
    } catch (err: any) {
      setError(err?.response?.data?.error ?? "Couldn't create the team from this selection.")
      setStep('name')
    }
  }

  const handleConfirmImport = async () => {
    if (!provider || !selectedItem || !selectedTeamId) return
    setStep('importing')
    try {
      const result = await provider.importIntoTeam(selectedItem, selectedTeamId, autoSync)
      setImportSummary(result)
      setStep('summary')
    } catch {
      showToast('Import failed.', 'error')
      setStep(provider.previewImport ? 'preview' : 'team')
    }
  }

  const handlePickExistingTeam = async () => {
    if (!provider || !selectedItem || !selectedTeamId) return
    if (!provider.previewImport) {
      // GitHub/Azure DevOps/GitLab skip straight to importing — only Jira supports a preview.
      await handleConfirmImport()
      return
    }
    setStep('importing') // reused briefly as a "loading preview" indicator
    try {
      const rows = await provider.previewImport(selectedItem)
      setPreview(rows)
      setStep('preview')
    } catch {
      showToast("Couldn't load a preview for this selection.", 'error')
      setStep('team')
    }
  }

  const handleGoToTeam = () => {
    const teamId = createResult?.team.id ?? selectedTeamId
    if (!teamId) return
    onClose()
    navigate(`/teams/${teamId}`)
  }

  const handleBackToSource = () => {
    setProvider(null)
    setItems([])
    setSelectedItem(null)
    setError(null)
    setStep('source')
  }

  const validCount = preview?.filter((r) => r.isValid).length ?? 0
  const invalidCount = (preview?.length ?? 0) - validCount

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" ref={containerRef} role="dialog" aria-modal="true" style={{ maxWidth: 640 }} onClick={(e) => e.stopPropagation()}>
        <h2>Import a team</h2>

        {step === 'source' && (
          <>
            <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 12 }}>
              Where should this team's issues come from?
            </p>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
              {importProviders.map((p) => (
                <button key={p.id} className="btn" style={{ justifyContent: 'flex-start' }} onClick={() => handlePickProvider(p)}>
                  {p.displayName}
                </button>
              ))}
            </div>
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={onClose}>Cancel</button>
            </div>
          </>
        )}

        {step === 'checking' && provider && <p>Checking your {provider.displayName} connection…</p>}

        {step === 'notConnected' && provider && (
          <>
            <p style={{ fontSize: 13, marginBottom: 12 }}>
              You need to connect {provider.displayName} before you can import a {provider.itemNoun} from it.
            </p>
            <div className="modal-actions">
              <button className="btn" onClick={handleBackToSource}>Back</button>
              <button className="btn btn-primary" onClick={() => { onClose(); navigate('/settings') }}>Go to Settings</button>
            </div>
          </>
        )}

        {step === 'item' && provider && (
          <>
            <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 12 }}>
              Pick a {provider.displayName} {provider.itemNoun}.
            </p>
            {items.length === 0 && <p>{provider.noItemsMessage}</p>}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6, maxHeight: 320, overflowY: 'auto' }}>
              {items.map((item) => (
                <button
                  key={item.key}
                  className="btn"
                  style={{ justifyContent: 'flex-start', textAlign: 'left' }}
                  onClick={() => handlePickItem(item)}
                >
                  {item.label}
                </button>
              ))}
            </div>
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={handleBackToSource}>Back</button>
            </div>
          </>
        )}

        {step === 'destination' && provider && selectedItem && (
          <>
            <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 12 }}>
              Import <strong>{selectedItem.label}</strong> as…
            </p>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
              <button className="btn" style={{ justifyContent: 'flex-start' }} onClick={() => handlePickDestination('new')}>
                A brand-new team
              </button>
              <button className="btn" style={{ justifyContent: 'flex-start' }} onClick={() => handlePickDestination('existing')}>
                Into a team you already have
              </button>
            </div>
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={() => setStep('item')}>Back</button>
            </div>
          </>
        )}

        {/* New-team path */}
        {step === 'name' && provider && selectedItem && (
          <>
            <div className="field">
              <label htmlFor="import-team-name">Team name</label>
              <input id="import-team-name" className="input" value={teamName} onChange={(e) => setTeamName(e.target.value)} maxLength={50} autoFocus />
            </div>
            <p style={{ fontSize: 11.5, color: 'var(--color-ink-faint)', marginTop: 4 }}>
              {provider.infoText(selectedItem)}
            </p>
            {provider.supportsAutoSync && (
              <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 8, fontSize: 12.5 }}>
                <input type="checkbox" checked={autoSync} onChange={(e) => setAutoSync(e.target.checked)} />
                Keep this team in sync with {provider.displayName} (re-imports automatically every few hours)
              </label>
            )}
            {error && <p className="field-error" role="alert">{error}</p>}
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={() => setStep('destination')}>Back</button>
              <button className="btn btn-primary" disabled={!teamName.trim()} onClick={handleCreateNewTeam}>Create team</button>
            </div>
          </>
        )}

        {step === 'creating' && <p>Creating your team and importing…</p>}

        {/* Existing-team path */}
        {step === 'team' && provider && selectedItem && (
          <>
            <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 12 }}>
              Import <strong>{selectedItem.label}</strong> into which team's backlog?
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
              {provider.infoText(selectedItem)} Re-importing later updates existing stories instead of duplicating them.
            </p>
            {provider.supportsAutoSync && (
              <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 12, fontSize: 12.5 }}>
                <input type="checkbox" checked={autoSync} onChange={(e) => setAutoSync(e.target.checked)} />
                Keep this team in sync with {provider.displayName} (re-imports automatically every few hours)
              </label>
            )}
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={() => setStep('destination')}>Back</button>
              <button className="btn btn-primary" disabled={!selectedTeamId} onClick={handlePickExistingTeam}>
                {provider.previewImport ? 'Next: Preview' : 'Import'}
              </button>
            </div>
          </>
        )}

        {step === 'preview' && preview && (
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
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={() => setStep('team')}>Back</button>
              <button className="btn btn-primary" disabled={validCount === 0} onClick={handleConfirmImport}>
                Import {validCount} stories
              </button>
            </div>
          </>
        )}

        {step === 'importing' && <p>{preview ? 'Importing…' : 'Loading a preview…'}</p>}

        {step === 'summary' && (createResult || importSummary) && (
          <>
            {createResult ? (
              <p style={{ marginTop: 4 }}>
                <strong>"{createResult.team.name}"</strong> was created with <strong>{createResult.importSummary.createdCount}</strong> stories.
                {createResult.importSummary.updatedCount > 0 && ` ${createResult.importSummary.updatedCount} existing stories updated.`}
                {createResult.importSummary.skippedCount > 0 && ` ${createResult.importSummary.skippedCount} row(s) were skipped.`}
              </p>
            ) : importSummary && (
              <p style={{ marginTop: 4 }}>
                <strong>{importSummary.createdCount}</strong> stories created.
                {importSummary.updatedCount > 0 && ` ${importSummary.updatedCount} existing stories updated.`}
                {importSummary.skippedCount > 0 && ` ${importSummary.skippedCount} row(s) skipped.`}
              </p>
            )}
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
