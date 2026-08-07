import { useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { DndContext, closestCenter, useSensor, useSensors, PointerSensor, TouchSensor, type DragEndEvent } from '@dnd-kit/core'
import { userStoriesApi } from '@/api/userStories'
import { sprintsApi } from '@/api/sprints'
import { boardsApi, type Board } from '@/api/boards'
import BoardTabs from '@/components/board/BoardTabs'
import type { UserStory, UserStoryStatus } from '@/types/userStory'
import type { Sprint } from '@/types/sprint'
import BoardColumn from '@/components/board/BoardColumn'
import BoardFilterBar, { type BoardFilters } from '@/components/board/BoardFilterBar'
import EditUserStoryModal from '@/components/userStories/EditUserStoryModal'
import { SkeletonBoard } from '@/components/common/Skeleton'
import { ensureRealtimeConnectionStarted, getRealtimeConnection } from '@/services/realtimeConnection'
import { useUserNames } from '@/hooks/useUserNames'
import { useToast } from '@/context/ToastContext'
import type { TeamOutletContext } from './TeamShellPage'

const COLUMNS: { status: UserStoryStatus; title: string }[] = [
  { status: 'ToDo', title: 'To Do' },
  { status: 'Analyze', title: 'Analyze' },
  { status: 'Dev', title: 'Dev' },
  { status: 'Test', title: 'Test' },
  { status: 'Debug', title: 'Debug' },
  { status: 'Done', title: 'Done' },
]

export default function BoardPage() {
  const { team } = useOutletContext<TeamOutletContext>()
  const { showToast } = useToast()
  const [stories, setStories] = useState<UserStory[]>([])
  const [isLoading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [filters, setFilters] = useState<BoardFilters>({})
  const [editingStory, setEditingStory] = useState<UserStory | null>(null)
  const [sprints, setSprints] = useState<Sprint[]>([])
  const [sprintFilter, setSprintFilter] = useState('')
  const [boards, setBoards] = useState<Board[]>([])
  const [selectedBoardId, setSelectedBoardId] = useState<string | null>(null)

  const loadBoards = () => boardsApi.getByTeam(team.id).then(setBoards)

  const handleSelectBoard = (boardId: string | null) => {
    setSelectedBoardId(boardId)
    const board = boardId ? boards.find((b) => b.id === boardId) : null
    setSprintFilter(board?.sprintId ?? '')
  }

  // PointerSensor covers mouse/trackpad; TouchSensor makes cards draggable
  // on touchscreens too (a small delay + tolerance so a tap-to-open doesn't
  // get mistaken for the start of a drag).
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(TouchSensor, {
      activationConstraint: { delay: 150, tolerance: 5 },
    })
  )

  const userNames = useUserNames([
    ...team.members.map((m) => m.userId),
    ...stories.map((s) => s.assigneeId),
  ])

  const visibleStories = stories.filter((story) => {
    if (filters.priority && story.priority !== filters.priority) return false
    if (filters.assigneeId && story.assigneeId !== filters.assigneeId) return false
    if (filters.keyword) {
      const keyword = filters.keyword.toLowerCase()
      const matchesTitle = story.title.toLowerCase().includes(keyword)
      const matchesDescription = story.description?.toLowerCase().includes(keyword) ?? false
      if (!matchesTitle && !matchesDescription) return false
    }
    return true
  })

  const loadStories = () => {
    // The board shows the whole backlog at once (no pagination UI), so we
    // request a generously large page. A truly huge backlog would need
    // per-column virtualization instead — out of scope for this skeleton.
    userStoriesApi.getByTeam(team.id, {}, 1, 500, false, sprintFilter || undefined).then((result) => setStories(result.items))
  }

  useEffect(() => {
    sprintsApi.getForTeam(team.id).then(setSprints)
    loadBoards()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [team.id])

  useEffect(() => {
    setLoading(true)
    loadStories()
    setLoading(false)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [team.id, sprintFilter])

  // Join this team's realtime group so we get pushed a "teamUpdate" event
  // whenever anyone (including us, in another tab) changes a story — the
  // handler just refetches rather than trying to patch state precisely.
  useEffect(() => {
    let isActive = true
    const connection = getRealtimeConnection()

    const handleTeamUpdate = () => {
      if (isActive) loadStories()
    }

    ensureRealtimeConnectionStarted()
      .then(() => connection.invoke('JoinTeam', team.id))
      .catch(() => {
        // If the realtime connection can't be established, the board simply
        // won't live-update — the user can still refresh manually.
      })

    // withAutomaticReconnect() re-establishes the socket but doesn't remember
    // which team groups we were in — rejoin explicitly once it's back.
    const rejoin = () => { connection.invoke('JoinTeam', team.id).catch(() => {}) }
    connection.onreconnected(rejoin)

    connection.on('teamUpdate', handleTeamUpdate)

    return () => {
      isActive = false
      connection.off('teamUpdate', handleTeamUpdate)
      connection.invoke('LeaveTeam', team.id).catch(() => {})
    }
    // sprintFilter is included so this effect re-subscribes with a fresh
    // handleTeamUpdate closure whenever it changes — otherwise a live update
    // pushed after switching sprints would refetch using the OLD filter.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [team.id, sprintFilter])

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event
    if (!over) return

    const storyId = String(active.id)
    const newStatus = over.id as UserStoryStatus
    const story = stories.find((s) => s.id === storyId)
    if (!story || story.status === newStatus) return

    const previousStatus = story.status

    setStories((prev) => prev.map((s) => (s.id === storyId ? { ...s, status: newStatus } : s)))

    try {
      await userStoriesApi.changeStatus(storyId, newStatus)
      setError(null)
    } catch {
      setStories((prev) => prev.map((s) => (s.id === storyId ? { ...s, status: previousStatus } : s)))
      setError(`Can't move "${story.title}" from ${previousStatus} directly to ${newStatus}.`)
    }
  }

  const handleSaveEdit = async (title: string, description: string, dueDate: string | undefined, storyPoints: number | undefined) => {
    if (!editingStory) return
    try {
      await userStoriesApi.update(editingStory.id, title, description || undefined, dueDate, storyPoints, editingStory.version)
      setEditingStory(null)
      loadStories()
      showToast('Changes saved.')
    } catch {
      showToast('Could not save — someone else may have edited this story. Refresh and try again.', 'error')
    }
  }

  // Refreshes both the board (so the card's chips update) and the panel's
  // own copy of the story (so its label toggles reflect what just saved,
  // without closing the panel).
  const handleLabelsChanged = async () => {
    if (!editingStory) return
    const fresh = await userStoriesApi.getById(editingStory.id)
    setEditingStory(fresh)
    loadStories()
  }

  if (isLoading) return <SkeletonBoard />

  return (
    <div>
      {error && <div className="alert-error" role="alert">{error}</div>}

      <BoardTabs
        teamId={team.id}
        boards={boards}
        sprints={sprints}
        selectedBoardId={selectedBoardId}
        onSelect={handleSelectBoard}
        onChanged={loadBoards}
      />

      {sprints.length > 0 && (
        <div style={{ marginBottom: 12 }}>
          <select className="pill-select" value={sprintFilter} onChange={(e) => setSprintFilter(e.target.value)}>
            <option value="">Whole backlog (all sprints)</option>
            {sprints.map((s) => (
              <option key={s.id} value={s.id}>{s.name} ({s.status})</option>
            ))}
          </select>
        </div>
      )}

      <BoardFilterBar
        members={team.members}
        userNames={userNames}
        filters={filters}
        onChange={setFilters}
      />

      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <div className="board">
          {COLUMNS.map((col) => (
            <BoardColumn
              key={col.status}
              status={col.status}
              title={col.title}
              teamName={team.name}
              userNames={userNames}
              labels={team.labels}
              wipLimit={team.wipLimits.find((w) => w.status === col.status)?.limit}
              stories={visibleStories.filter((s) => s.status === col.status)}
              onOpenPanel={setEditingStory}
            />
          ))}
        </div>
      </DndContext>

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
    </div>
  )
}
