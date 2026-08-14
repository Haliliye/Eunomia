import { useEffect, useRef, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { userStoriesApi, type UserStoryFilters } from '@/api/userStories'
import { sprintsApi } from '@/api/sprints'
import type { Sprint } from '@/types/sprint'
import type { UserStory, UserStoryStatus, UserStoryPriority } from '@/types/userStory'
import UserStoryList from '@/components/userStories/UserStoryList'
import UserStoryFilterBar from '@/components/userStories/UserStoryFilterBar'
import CreateUserStoryModal from '@/components/userStories/CreateUserStoryModal'
import ImportCsvModal from '@/components/userStories/ImportCsvModal'
import BulkCreateModal from '@/components/userStories/BulkCreateModal'
import EditUserStoryModal from '@/components/userStories/EditUserStoryModal'
import { useToast } from '@/context/ToastContext'
import { useConfirm } from '@/context/ConfirmContext'
import { useAuth } from '@/context/AuthContext'
import { getSavedFilters, saveFilter, deleteSavedFilter, type SavedFilter } from '@/lib/savedFilters'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'
import { useUserNames } from '@/hooks/useUserNames'
import { useKeyboardShortcut } from '@/hooks/useKeyboardShortcut'
import { ensureRealtimeConnectionStarted, getRealtimeConnection } from '@/services/realtimeConnection'
import { SkeletonTable } from '@/components/common/Skeleton'
import type { TeamOutletContext } from './TeamShellPage'

function filterStorageKey(teamId: string) {
  return `todoapp:filters:${teamId}`
}

function loadFilters(teamId: string): UserStoryFilters {
  try {
    const raw = sessionStorage.getItem(filterStorageKey(teamId))
    return raw ? JSON.parse(raw) : {}
  } catch {
    return {}
  }
}

const PAGE_SIZE = 25

export default function TeamBacklogPage() {
  const { team } = useOutletContext<TeamOutletContext>()
  const { user } = useAuth()
  const { showToast } = useToast()
  const confirm = useConfirm()
  const [stories, setStories] = useState<UserStory[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [filters, setFilters] = useState<UserStoryFilters>(() => loadFilters(team.id))
  const [savedFilters, setSavedFilters] = useState<SavedFilter[]>(() => user ? getSavedFilters(user.userId, team.id) : [])
  const [isSaveFilterOpen, setSaveFilterOpen] = useState(false)
  const [saveFilterName, setSaveFilterName] = useState('')
  useEscapeToClose(isSaveFilterOpen, () => setSaveFilterOpen(false))
  const saveFilterModalRef = useFocusTrap(isSaveFilterOpen)
  const [isLoading, setLoading] = useState(true)
  const [isCreateOpen, setCreateOpen] = useState(false)
  const [isImportOpen, setImportOpen] = useState(false)
  const [isBulkCreateOpen, setBulkCreateOpen] = useState(false)
  const [editingStory, setEditingStory] = useState<UserStory | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [sprints, setSprints] = useState<Sprint[]>([])
  const [sprintFilter, setSprintFilter] = useState<string>('')
  const [labelFilter, setLabelFilter] = useState<string>('')

  const userNames = useUserNames([
    ...team.members.map((m) => m.userId),
    ...stories.map((s) => s.assigneeId),
  ])

  const loadStories = (targetPage: number) => {
    setLoading(true)
    userStoriesApi.getByTeam(team.id, filters, targetPage, PAGE_SIZE, false, sprintFilter || undefined, labelFilter || undefined)
      .then((result) => {
        setStories(result.items)
        setTotalCount(result.totalCount)
        setPage(result.page)
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    sprintsApi.getForTeam(team.id).then(setSprints)
  }, [team.id])

  // Reset to page 1 whenever the team or filters change — a filter change
  // easily makes the previous page number point past the new, smaller result set.
  useEffect(() => {
    loadStories(1)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [team.id, filters, sprintFilter, labelFilter])

  const handleFiltersChange = (next: UserStoryFilters) => {
    setFilters(next)
    sessionStorage.setItem(filterStorageKey(team.id), JSON.stringify(next))
  }

  const handleApplySavedFilter = (saved: SavedFilter) => {
    handleFiltersChange(saved.filters)
    setSprintFilter(saved.sprintId ?? '')
    setLabelFilter(saved.labelId ?? '')
  }

  const handleSaveCurrentFilters = () => {
    if (!user || !saveFilterName.trim()) return
    const next = saveFilter(user.userId, team.id, saveFilterName.trim(), filters, sprintFilter || undefined, labelFilter || undefined)
    setSavedFilters(next)
    setSaveFilterName('')
    setSaveFilterOpen(false)
    showToast(`Saved view "${saveFilterName.trim()}".`)
  }

  const handleDeleteSavedFilter = (id: string, name: string) => {
    if (!user) return
    setSavedFilters(deleteSavedFilter(user.userId, team.id, id))
    showToast(`Removed saved view "${name}".`)
  }

  const handleMyStories = () => {
    if (!user) return
    handleFiltersChange({ ...filters, assigneeId: filters.assigneeId === user.userId ? undefined : user.userId })
  }

  const refetchStories = () => loadStories(page)
  const pageRef = useRef(page)
  pageRef.current = page

  // Live refresh: anyone else's change to this team's stories (or our own
  // action in another tab/board view) refetches the current page. Uses
  // pageRef (not the `page` state closed over above) because this effect
  // only re-subscribes on team change — without the ref it would keep
  // refetching whatever page was current when the subscription was created.
  useEffect(() => {
    let isActive = true
    const connection = getRealtimeConnection()
    const handleTeamUpdate = () => { if (isActive) loadStories(pageRef.current) }

    ensureRealtimeConnectionStarted()
      .then(() => connection.invoke('JoinTeam', team.id))
      .catch(() => {})

    const rejoin = () => { connection.invoke('JoinTeam', team.id).catch(() => {}) }
    connection.onreconnected(rejoin)

    connection.on('teamUpdate', handleTeamUpdate)

    return () => {
      isActive = false
      connection.off('teamUpdate', handleTeamUpdate)
      connection.invoke('LeaveTeam', team.id).catch(() => {})
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [team.id])

  const handleCreate = async (title: string, description: string, priority?: string, checklistItemTexts?: string[]) => {
    const story = await userStoriesApi.create(team.id, title, description || undefined)

    if (priority) {
      await userStoriesApi.changePriority(story.id, priority as UserStoryPriority)
    }
    if (checklistItemTexts && checklistItemTexts.length > 0) {
      for (const text of checklistItemTexts) {
        await userStoriesApi.addChecklistItem(story.id, text)
      }
    }

    await refetchStories()
    setCreateOpen(false)
    setError(null)
    showToast(`"${title}" was created.`)
  }

  const handleSaveEdit = async (title: string, description: string, dueDate: string | undefined, storyPoints: number | undefined) => {
    if (!editingStory) return
    try {
      await userStoriesApi.update(editingStory.id, title, description || undefined, dueDate, storyPoints, editingStory.version)
      await refetchStories()
      setEditingStory(null)
      setError(null)
      showToast('Changes saved.')
    } catch (err) {
      setError(extractErrorMessage(err))
    }
  }

  // Refreshes both the backlog list (so the row's chips update) and the
  // modal's own copy of the story (so its label toggles reflect what just
  // saved, without closing the modal).
  const handleLabelsChanged = async () => {
    if (!editingStory) return
    const fresh = await userStoriesApi.getById(editingStory.id)
    setEditingStory(fresh)
    await refetchStories()
  }

  const handleBulkCreate = async (titles: string[]) => {
    try {
      await userStoriesApi.bulkCreate(team.id, titles)
      await refetchStories()
      setBulkCreateOpen(false)
      showToast(`${titles.length} ${titles.length === 1 ? 'story' : 'stories'} created.`)
    } catch (err) {
      showToast('Could not create those stories.', 'error')
      throw err
    }
  }

  const handleDeleteStory = async (story: UserStory) => {
    const confirmed = await confirm({ title: `Delete "${story.title}"?`, confirmLabel: 'Delete', danger: true })
    if (!confirmed) return

    try {
      await userStoriesApi.delete(story.id)
      await refetchStories()
      setError(null)
      showToast(`"${story.title}" was deleted.`)
    } catch (err) {
      setError(extractErrorMessage(err))
    }
  }

  const handleArchive = async (story: UserStory) => {
    try {
      await userStoriesApi.archive(story.id)
      await refetchStories()
      setError(null)
      showToast(`"${story.title}" was archived.`, 'success', {
        label: 'Undo',
        onClick: async () => {
          try {
            await userStoriesApi.unarchive(story.id)
            await refetchStories()
            showToast(`"${story.title}" restored.`)
          } catch {
            showToast("Couldn't undo that.", 'error')
          }
        },
      })
    } catch (err) {
      setError(extractErrorMessage(err))
    }
  }

  const handleStatusChange = async (story: UserStory, status: UserStoryStatus) => {
    try {
      await userStoriesApi.changeStatus(story.id, status)
      await refetchStories()
      setError(null)
    } catch (err) {
      setError(extractErrorMessage(err))
    }
  }

  const handlePriorityChange = async (story: UserStory, priority: UserStoryPriority) => {
    try {
      await userStoriesApi.changePriority(story.id, priority)
      await refetchStories()
      setError(null)
    } catch (err) {
      setError(extractErrorMessage(err))
    }
  }

  const handleAssigneeChange = async (story: UserStory, assigneeId: string | null) => {
    try {
      await userStoriesApi.assign(story.id, assigneeId)
      await refetchStories()
      setError(null)
    } catch (err) {
      setError(extractErrorMessage(err))
    }
  }

  const toggleSelect = (storyId: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev)
      if (next.has(storyId)) next.delete(storyId)
      else next.add(storyId)
      return next
    })
  }

  // Implemented as parallel individual requests rather than a new bulk
  // backend endpoint — the existing single-item archive/status endpoints
  // already do everything needed; N requests in parallel is simple and fast
  // enough for a "select a handful of rows" action. A true bulk endpoint
  // would matter more at hundreds of selected rows, not tens.
  const handleBulkArchive = async () => {
    const ids = Array.from(selectedIds)
    if (ids.length === 0) return
    const confirmed = await confirm({ title: `Archive ${ids.length} selected stories?`, confirmLabel: 'Archive' })
    if (!confirmed) return

    const results = await Promise.allSettled(ids.map((id) => userStoriesApi.archive(id)))
    const failedCount = results.filter((r) => r.status === 'rejected').length

    setSelectedIds(new Set())
    await refetchStories()

    if (failedCount > 0) {
      setError(`${failedCount} of ${ids.length} stories couldn't be archived.`)
    } else {
      const successfulIds = ids
      showToast(`${ids.length} stories archived.`, 'success', {
        label: 'Undo',
        onClick: async () => {
          try {
            await Promise.allSettled(successfulIds.map((id) => userStoriesApi.unarchive(id)))
            await refetchStories()
            showToast(`${successfulIds.length} stories restored.`)
          } catch {
            showToast("Couldn't undo that.", 'error')
          }
        },
      })
    }
  }

  const handleBulkStatusChange = async (status: UserStoryStatus) => {
    const ids = Array.from(selectedIds)
    if (ids.length === 0) return

    const results = await Promise.allSettled(ids.map((id) => userStoriesApi.changeStatus(id, status)))
    const failedCount = results.filter((r) => r.status === 'rejected').length

    setSelectedIds(new Set())
    await refetchStories()

    if (failedCount > 0) {
      setError(`${failedCount} of ${ids.length} stories couldn't move to ${status} (an invalid transition for their current status).`)
    } else {
      showToast(`${ids.length} stories moved to ${status}.`)
    }
  }

  useKeyboardShortcut('c', () => setCreateOpen(true))
  useKeyboardShortcut('/', () => document.getElementById('backlog-search-input')?.focus())

  const handleBulkMoveToSprint = async (sprintId: string) => {
    const ids = Array.from(selectedIds)
    if (ids.length === 0) return

    await Promise.all(ids.map((id) => userStoriesApi.moveToSprint(id, sprintId || null)))
    setSelectedIds(new Set())
    await refetchStories()
    showToast(sprintId ? `${ids.length} stories moved to sprint.` : `${ids.length} stories moved back to the backlog.`)
  }

  const handleBulkAssign = async (assigneeId: string) => {
    const ids = Array.from(selectedIds)
    if (ids.length === 0) return

    const results = await Promise.allSettled(ids.map((id) => userStoriesApi.assign(id, assigneeId || null)))
    const failedCount = results.filter((r) => r.status === 'rejected').length

    setSelectedIds(new Set())
    await refetchStories()

    if (failedCount > 0) {
      setError(`${failedCount} of ${ids.length} stories couldn't be reassigned.`)
    } else {
      showToast(`${ids.length} stories reassigned.`)
    }
  }

  const handleBulkSetDueDate = async (dueDate: string) => {
    const ids = Array.from(selectedIds)
    if (ids.length === 0) return

    const results = await Promise.allSettled(ids.map((id) => userStoriesApi.setDueDate(id, dueDate || null)))
    const failedCount = results.filter((r) => r.status === 'rejected').length

    setSelectedIds(new Set())
    await refetchStories()

    if (failedCount > 0) {
      setError(`${failedCount} of ${ids.length} stories couldn't be updated.`)
    } else {
      showToast(dueDate ? `Due date set on ${ids.length} stories.` : `Due date cleared on ${ids.length} stories.`)
    }
  }

  const handleBulkPriorityChange = async (priority: UserStoryPriority) => {
    const ids = Array.from(selectedIds)
    if (ids.length === 0) return

    const results = await Promise.allSettled(ids.map((id) => userStoriesApi.changePriority(id, priority)))
    const failedCount = results.filter((r) => r.status === 'rejected').length

    setSelectedIds(new Set())
    await refetchStories()

    if (failedCount > 0) {
      setError(`${failedCount} of ${ids.length} stories couldn't have their priority changed.`)
    } else {
      showToast(`${ids.length} stories set to ${priority} priority.`)
    }
  }

  const handleBulkAddLabel = async (labelId: string) => {
    const ids = Array.from(selectedIds)
    if (ids.length === 0) return

    const results = await Promise.allSettled(ids.map((id) => userStoriesApi.addLabel(id, labelId)))
    const failedCount = results.filter((r) => r.status === 'rejected').length

    setSelectedIds(new Set())
    await refetchStories()

    if (failedCount > 0) {
      setError(`${failedCount} of ${ids.length} stories couldn't be labeled.`)
    } else {
      showToast(`Label applied to ${ids.length} stories.`)
    }
  }

  const handleExport = async () => {
    try {
      await userStoriesApi.exportCsv(team.id, filters, sprintFilter || undefined, labelFilter || undefined, false)
    } catch {
      showToast('Could not export stories.', 'error')
    }
  }

  return (
    <div>
      {error && <div className="alert-error" role="alert">{error}</div>}

      <div className="backlog-header">
        <div className="backlog-header-title">
          <span className="backlog-chevron">▾</span>
          Backlog
          <span className="backlog-count">({totalCount} work item{totalCount === 1 ? '' : 's'})</span>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <button
            className={`btn btn-sm ${filters.assigneeId === user?.userId ? 'btn-primary' : ''}`}
            onClick={handleMyStories}
          >
            My stories
          </button>
          {savedFilters.map((f) => (
            <span key={f.id} className="label-chip" style={{ display: 'inline-flex', alignItems: 'center', gap: 4, background: 'var(--color-surface-sunken)', border: '1px solid var(--color-border)' }}>
              <button
                style={{ background: 'none', border: 'none', padding: 0, font: 'inherit', color: 'inherit', cursor: 'pointer' }}
                onClick={() => handleApplySavedFilter(f)}
              >
                ★ {f.name}
              </button>
              <button
                style={{ background: 'none', border: 'none', padding: 0, font: 'inherit', color: 'inherit', opacity: 0.6, cursor: 'pointer' }}
                title="Remove this saved view"
                onClick={() => handleDeleteSavedFilter(f.id, f.name)}
              >
                ✕
              </button>
            </span>
          ))}
          <button className="btn btn-sm" onClick={() => setSaveFilterOpen(true)}>☆ Save current filters</button>
          <select className="pill-select" value={sprintFilter} onChange={(e) => setSprintFilter(e.target.value)}>
            <option value="">All stories</option>
            <option value="none">Backlog only (no sprint)</option>
            {sprints.map((s) => (
              <option key={s.id} value={s.id}>{s.name} ({s.status})</option>
            ))}
          </select>
          {team.labels.length > 0 && (
            <select className="pill-select" value={labelFilter} onChange={(e) => setLabelFilter(e.target.value)}>
              <option value="">All labels</option>
              {team.labels.map((l) => (
                <option key={l.id} value={l.id}>{l.name}</option>
              ))}
            </select>
          )}
          <button className="btn btn-sm" onClick={handleExport}>⬇ Export CSV</button>
          <button className="btn btn-sm" onClick={() => setImportOpen(true)}>⬆ Import CSV</button>
          <button className="btn btn-sm" onClick={() => setBulkCreateOpen(true)}>+ Add multiple</button>
          <button className="btn btn-primary btn-sm" title="Shortcut: c" onClick={() => setCreateOpen(true)}>+ Create story</button>
        </div>
      </div>

      <UserStoryFilterBar
        members={team.members}
        columns={team.columns}
        userNames={userNames}
        filters={filters}
        onChange={handleFiltersChange}
      />

      {selectedIds.size > 0 && (
        <div className="filter-bar" style={{ background: 'var(--color-brand-soft)', padding: '8px 12px', borderRadius: 8, marginBottom: 12 }}>
          <span className="mono" style={{ fontSize: 12.5 }}>{selectedIds.size} selected</span>
          <button className="btn btn-sm" onClick={handleBulkArchive}>📦 Archive selected</button>
          <select
            className="pill-select"
            defaultValue=""
            onChange={(e) => {
              if (e.target.value) handleBulkStatusChange(e.target.value as UserStoryStatus)
              e.target.value = ''
            }}
          >
            <option value="" disabled>Move to status…</option>
            <option value="ToDo">To Do</option>
            <option value="Analyze">Analyze</option>
            <option value="Dev">Dev</option>
            <option value="Test">Test</option>
            <option value="Debug">Debug</option>
            <option value="Done">Done</option>
          </select>
          {sprints.length > 0 && (
            <select
              className="pill-select"
              defaultValue=""
              onChange={(e) => {
                if (e.target.value) handleBulkMoveToSprint(e.target.value === '__backlog__' ? '' : e.target.value)
                e.target.value = ''
              }}
            >
              <option value="" disabled>Move to sprint…</option>
              <option value="__backlog__">Backlog (no sprint)</option>
              {sprints.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
          )}
          <select
            className="pill-select"
            defaultValue=""
            onChange={(e) => {
              if (e.target.value) handleBulkPriorityChange(e.target.value as UserStoryPriority)
              e.target.value = ''
            }}
          >
            <option value="" disabled>Set priority…</option>
            <option value="Critical">Critical</option>
            <option value="High">High</option>
            <option value="Medium">Medium</option>
            <option value="Low">Low</option>
          </select>
          <select
            className="pill-select"
            defaultValue=""
            onChange={(e) => {
              if (e.target.value) handleBulkAssign(e.target.value === '__unassigned__' ? '' : e.target.value)
              e.target.value = ''
            }}
          >
            <option value="" disabled>Assign to…</option>
            <option value="__unassigned__">Unassigned</option>
            {team.members.map((m) => (
              <option key={m.userId} value={m.userId}>{userNames[m.userId] ?? m.userId}</option>
            ))}
          </select>
          <input
            type="date"
            className="pill-select"
            style={{ width: 150 }}
            title="Set due date on selected stories"
            onChange={(e) => {
              if (e.target.value) handleBulkSetDueDate(e.target.value)
              e.target.value = ''
            }}
          />
          {team.labels.length > 0 && (
            <select
              className="pill-select"
              defaultValue=""
              onChange={(e) => {
                if (e.target.value) handleBulkAddLabel(e.target.value)
                e.target.value = ''
              }}
            >
              <option value="" disabled>Apply label…</option>
              {team.labels.map((l) => (
                <option key={l.id} value={l.id}>{l.name}</option>
              ))}
            </select>
          )}
          <button className="btn btn-ghost btn-sm" onClick={() => setSelectedIds(new Set())}>Clear selection</button>
        </div>
      )}

      {isLoading ? (
        <SkeletonTable />
      ) : (
        <>
          <UserStoryList
            teamName={team.name}
            stories={stories}
            members={team.members}
            labels={team.labels}
            columns={team.columns}
            userNames={userNames}
            hasActiveFilters={Boolean(filters.status || filters.priority || filters.assigneeId || filters.keyword || sprintFilter || labelFilter)}
            onCreateNew={() => setCreateOpen(true)}
            onClearFilters={() => { setFilters({}); setSprintFilter(''); setLabelFilter('') }}
            onEdit={setEditingStory}
            onDelete={handleDeleteStory}
            onArchive={handleArchive}
            onStatusChange={handleStatusChange}
            onPriorityChange={handlePriorityChange}
            onAssigneeChange={handleAssigneeChange}
            selectedIds={selectedIds}
            onToggleSelect={toggleSelect}
          />
          {totalCount > PAGE_SIZE && (
            <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginTop: 16 }}>
              <button className="btn btn-sm" disabled={page <= 1} onClick={() => loadStories(page - 1)}>← Prev</button>
              <span className="mono" style={{ fontSize: 12.5 }}>
                {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, totalCount)} of {totalCount}
              </span>
              <button
                className="btn btn-sm"
                disabled={page * PAGE_SIZE >= totalCount}
                onClick={() => loadStories(page + 1)}
              >
                Next →
              </button>
            </div>
          )}
        </>
      )}

      <CreateUserStoryModal
        isOpen={isCreateOpen}
        templates={team.templates}
        onClose={() => setCreateOpen(false)}
        onCreate={handleCreate}
      />
      <ImportCsvModal
        isOpen={isImportOpen}
        teamId={team.id}
        onClose={() => setImportOpen(false)}
        onImported={refetchStories}
      />
      <BulkCreateModal
        isOpen={isBulkCreateOpen}
        onClose={() => setBulkCreateOpen(false)}
        onCreate={handleBulkCreate}
      />
      <EditUserStoryModal
        key={editingStory?.id ?? 'none'}
        story={editingStory}
        members={team.members}
        labels={team.labels}
        userNames={userNames}
        onClose={() => setEditingStory(null)}
        onSave={handleSaveEdit}
        onLabelsChanged={handleLabelsChanged}
      />
      {isSaveFilterOpen && (
        <div className="modal-overlay" onClick={() => setSaveFilterOpen(false)}>
          <div ref={saveFilterModalRef} className="modal" role="dialog" aria-modal="true" style={{ maxWidth: 380 }} onClick={(e) => e.stopPropagation()}>
            <h2>Save current filters</h2>
            <div className="field">
              <label htmlFor="save-filter-name">Name</label>
              <input
                id="save-filter-name"
                className="input"
                value={saveFilterName}
                onChange={(e) => setSaveFilterName(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') handleSaveCurrentFilters() }}
                placeholder="e.g. Overdue & mine"
                maxLength={50}
                autoFocus
              />
            </div>
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={() => setSaveFilterOpen(false)}>Cancel</button>
              <button className="btn btn-primary" disabled={!saveFilterName.trim()} onClick={handleSaveCurrentFilters}>Save</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

// Axios errors carry the middleware's { error: string } body in response.data.
function extractErrorMessage(err: unknown): string {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const response = (err as { response?: { data?: { error?: string } } }).response
    if (response?.data?.error) return response.data.error
  }
  return 'Something went wrong. Please try again.'
}
