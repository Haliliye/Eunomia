import { useState } from 'react'
import { userStoriesApi, type CsvAnalysis, type CsvColumnMapping, type ImportRow, type ImportSummary } from '@/api/userStories'
import { useToast } from '@/context/ToastContext'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'

interface ImportCsvModalProps {
  isOpen: boolean
  teamId: string
  onClose: () => void
  onImported: () => void
}

type Step = 'upload' | 'mapping' | 'value-mapping' | 'preview' | 'summary'

const OUR_STATUSES = ['ToDo', 'Analyze', 'Dev', 'Test', 'Debug', 'Done']
const OUR_PRIORITIES = ['Critical', 'High', 'Medium', 'Low']

// Recognizes common column names from Jira and Azure DevOps exports (as well
// as our own — see ExportUserStoriesQueryHandler) so most imports need
// little or no manual remapping. Case-insensitive, checked in order.
const COLUMN_CANDIDATES: Record<keyof Omit<CsvColumnMapping, 'statusValueMap' | 'priorityValueMap'>, string[]> = {
  titleColumn: ['title', 'summary', 'issue title'],
  descriptionColumn: ['description'],
  statusColumn: ['status', 'state'],
  priorityColumn: ['priority'],
  dueDateColumn: ['due date', 'target date', 'duedate'],
  storyPointsColumn: ['story points', 'story point estimate', 'effort', 'storypoints'],
  labelsColumn: ['labels', 'tags'],
}

// Best-effort guesses for the value-mapping step — Jira and Azure DevOps'
// own default vocabularies mapped onto ours. Anything not recognized here
// just starts unmapped (defaults to ToDo/Medium on the backend) and the
// person can still pick it manually.
const STATUS_SYNONYMS: Record<string, string> = {
  'to do': 'ToDo', 'backlog': 'ToDo', 'new': 'ToDo', 'open': 'ToDo',
  'in progress': 'Dev', 'doing': 'Dev', 'active': 'Dev', 'development': 'Dev',
  'in review': 'Test', 'code review': 'Test', 'testing': 'Test', 'qa': 'Test',
  'blocked': 'Debug', 'reopened': 'Debug',
  'done': 'Done', 'closed': 'Done', 'resolved': 'Done', 'completed': 'Done',
  'analyze': 'Analyze', 'analysis': 'Analyze', 'design': 'Analyze',
}
const PRIORITY_SYNONYMS: Record<string, string> = {
  'highest': 'Critical', 'blocker': 'Critical', 'critical': 'Critical', '1': 'Critical',
  'high': 'High', '2': 'High',
  'medium': 'Medium', 'normal': 'Medium', '3': 'Medium',
  'low': 'Low', 'lowest': 'Low', '4': 'Low', '5': 'Low',
}

function guessColumn(headers: string[], candidates: string[]): string | undefined {
  for (const candidate of candidates) {
    const match = headers.find((h) => h.trim().toLowerCase() === candidate)
    if (match) return match
  }
  return undefined
}

// US-147 extended: works with ANY CSV export (Jira, Azure DevOps, or our
// own), not just a fixed template — upload, map columns, map status/priority
// vocabulary if needed, preview, confirm.
export default function ImportCsvModal({ isOpen, teamId, onClose, onImported }: ImportCsvModalProps) {
  const { showToast } = useToast()
  const [step, setStep] = useState<Step>('upload')
  const [file, setFile] = useState<File | null>(null)
  const [analysis, setAnalysis] = useState<CsvAnalysis | null>(null)
  const [mapping, setMapping] = useState<CsvColumnMapping>({ titleColumn: '' })
  const [preview, setPreview] = useState<ImportRow[] | null>(null)
  const [summary, setSummary] = useState<ImportSummary | null>(null)
  const [isBusy, setBusy] = useState(false)

  useEscapeToClose(isOpen, onClose)
  const containerRef = useFocusTrap(isOpen)

  if (!isOpen) return null

  const reset = () => {
    setStep('upload')
    setFile(null)
    setAnalysis(null)
    setMapping({ titleColumn: '' })
    setPreview(null)
    setSummary(null)
    onClose()
  }

  const handleFileSelected = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0]
    if (!selected) return
    setFile(selected)
    setBusy(true)
    try {
      const result = await userStoriesApi.analyzeCsv(teamId, selected)
      setAnalysis(result)
      setMapping({
        titleColumn: guessColumn(result.headers, COLUMN_CANDIDATES.titleColumn) ?? '',
        descriptionColumn: guessColumn(result.headers, COLUMN_CANDIDATES.descriptionColumn),
        statusColumn: guessColumn(result.headers, COLUMN_CANDIDATES.statusColumn),
        priorityColumn: guessColumn(result.headers, COLUMN_CANDIDATES.priorityColumn),
        dueDateColumn: guessColumn(result.headers, COLUMN_CANDIDATES.dueDateColumn),
        storyPointsColumn: guessColumn(result.headers, COLUMN_CANDIDATES.storyPointsColumn),
        labelsColumn: guessColumn(result.headers, COLUMN_CANDIDATES.labelsColumn),
      })
      setStep('mapping')
    } catch {
      showToast('Could not read that file — make sure it\'s a valid CSV.', 'error')
    } finally {
      setBusy(false)
    }
  }

  const distinctValuesFor = (columnName: string | undefined): string[] => {
    if (!analysis || !columnName) return []
    const index = analysis.headers.indexOf(columnName)
    if (index === -1) return []
    const values = new Set<string>()
    for (const row of analysis.sampleRows) {
      const value = row[index]?.trim()
      if (value) values.add(value)
    }
    return Array.from(values).sort()
  }

  const handleMappingNext = () => {
    if (!mapping.titleColumn) {
      showToast('Pick which column holds the story title.', 'error')
      return
    }

    if (mapping.statusColumn || mapping.priorityColumn) {
      // Pre-fill best guesses so most imports need zero clicks here.
      const statusValues = distinctValuesFor(mapping.statusColumn)
      const priorityValues = distinctValuesFor(mapping.priorityColumn)
      setMapping((prev) => ({
        ...prev,
        statusValueMap: Object.fromEntries(statusValues.map((v) => [v, STATUS_SYNONYMS[v.toLowerCase()] ?? ''])),
        priorityValueMap: Object.fromEntries(priorityValues.map((v) => [v, PRIORITY_SYNONYMS[v.toLowerCase()] ?? ''])),
      }))
      setStep('value-mapping')
    } else {
      runPreview(mapping)
    }
  }

  const runPreview = async (finalMapping: CsvColumnMapping) => {
    if (!file) return
    setBusy(true)
    try {
      const rows = await userStoriesApi.previewImport(teamId, file, finalMapping)
      setPreview(rows)
      setStep('preview')
    } catch {
      showToast('Could not preview this import — check your column mapping.', 'error')
    } finally {
      setBusy(false)
    }
  }

  const handleConfirm = async () => {
    if (!file) return
    setBusy(true)
    try {
      const result = await userStoriesApi.confirmImport(teamId, file, mapping)
      setSummary(result)
      setStep('summary')
      onImported()
    } catch {
      showToast('Import failed.', 'error')
    } finally {
      setBusy(false)
    }
  }

  const statusValues = Object.keys(mapping.statusValueMap ?? {})
  const priorityValues = Object.keys(mapping.priorityValueMap ?? {})
  const validCount = preview?.filter((r) => r.isValid).length ?? 0
  const invalidCount = (preview?.length ?? 0) - validCount

  const ColumnSelect = ({ label, field, required }: { label: string; field: keyof CsvColumnMapping; required?: boolean }) => (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
      <label style={{ width: 110, fontSize: 13 }}>{label}{required && ' *'}</label>
      <select
        className="pill-select"
        value={(mapping[field] as string) ?? ''}
        onChange={(e) => setMapping((prev) => ({ ...prev, [field]: e.target.value || undefined }))}
        style={{ flex: 1 }}
      >
        {!required && <option value="">— Don't import —</option>}
        {required && <option value="" disabled>Choose a column…</option>}
        {analysis?.headers.map((h) => (
          <option key={h} value={h}>{h}</option>
        ))}
      </select>
    </div>
  )

  return (
    <div className="modal-overlay" onClick={reset}>
      <div className="modal" ref={containerRef} role="dialog" aria-modal="true" style={{ maxWidth: 640 }} onClick={(e) => e.stopPropagation()}>
        <h2>Import stories from CSV</h2>
        <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 12 }}>
          Works with a Jira export, an Azure DevOps export, or our own — map your columns below,
          whatever they're named.
        </p>

        {step === 'upload' && (
          <input type="file" accept=".csv,text/csv" onChange={handleFileSelected} disabled={isBusy} />
        )}

        {step === 'mapping' && analysis && (
          <>
            <ColumnSelect label="Title" field="titleColumn" required />
            <ColumnSelect label="Description" field="descriptionColumn" />
            <ColumnSelect label="Status" field="statusColumn" />
            <ColumnSelect label="Priority" field="priorityColumn" />
            <ColumnSelect label="Due date" field="dueDateColumn" />
            <ColumnSelect label="Story points" field="storyPointsColumn" />
            <ColumnSelect label="Labels" field="labelsColumn" />
            <p style={{ fontSize: 11.5, color: 'var(--color-ink-faint)', marginTop: 8 }}>
              Assignees aren't imported — an external tool's assignee (a name, not an email)
              can't be reliably matched to one of your team's accounts.
            </p>
          </>
        )}

        {step === 'value-mapping' && (
          <>
            {statusValues.length > 0 && (
              <>
                <h3 style={{ fontSize: 14 }}>Map status values</h3>
                {statusValues.map((value) => (
                  <div key={value} style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
                    <span className="mono" style={{ width: 140, fontSize: 12.5 }} title={value}>{value}</span>
                    <span style={{ color: 'var(--color-ink-faint)' }}>→</span>
                    <select
                      className="pill-select"
                      value={mapping.statusValueMap?.[value] ?? ''}
                      onChange={(e) => setMapping((prev) => ({ ...prev, statusValueMap: { ...prev.statusValueMap, [value]: e.target.value } }))}
                    >
                      <option value="">Default (To Do)</option>
                      {OUR_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
                    </select>
                  </div>
                ))}
              </>
            )}
            {priorityValues.length > 0 && (
              <>
                <h3 style={{ fontSize: 14, marginTop: 12 }}>Map priority values</h3>
                {priorityValues.map((value) => (
                  <div key={value} style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
                    <span className="mono" style={{ width: 140, fontSize: 12.5 }} title={value}>{value}</span>
                    <span style={{ color: 'var(--color-ink-faint)' }}>→</span>
                    <select
                      className="pill-select"
                      value={mapping.priorityValueMap?.[value] ?? ''}
                      onChange={(e) => setMapping((prev) => ({ ...prev, priorityValueMap: { ...prev.priorityValueMap, [value]: e.target.value } }))}
                    >
                      <option value="">Default (Medium)</option>
                      {OUR_PRIORITIES.map((p) => <option key={p} value={p}>{p}</option>)}
                    </select>
                  </div>
                ))}
              </>
            )}
          </>
        )}

        {isBusy && <p style={{ marginTop: 12 }}>Working…</p>}

        {step === 'preview' && preview && !isBusy && (
          <>
            <p style={{ fontSize: 13, margin: '12px 0' }}>
              <strong>{validCount}</strong> row(s) will be imported.
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
            {summary.skippedCount > 0 && ` ${summary.skippedCount} row(s) skipped.`}
          </p>
        )}

        <div className="modal-actions" style={{ marginTop: 16 }}>
          <button className="btn" onClick={reset}>{step === 'summary' ? 'Close' : 'Cancel'}</button>
          {step === 'mapping' && <button className="btn btn-primary" disabled={isBusy} onClick={handleMappingNext}>Next</button>}
          {step === 'value-mapping' && <button className="btn btn-primary" disabled={isBusy} onClick={() => runPreview(mapping)}>Next: Preview</button>}
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
