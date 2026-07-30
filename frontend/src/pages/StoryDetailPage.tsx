import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { userStoriesApi } from '@/api/userStories'
import { teamsApi } from '@/api/teams'
import type { UserStory, UserStoryStatus, UserStoryPriority } from '@/types/userStory'
import type { Team } from '@/types/team'
import StatusBadge from '@/components/common/StatusBadge'
import PriorityBadge from '@/components/common/PriorityBadge'
import EditUserStoryModal from '@/components/userStories/EditUserStoryModal'
import CommentSection from '@/components/comments/CommentSection'
import Checklist from '@/components/userStories/Checklist'
import LabelChip from '@/components/common/LabelChip'
import RecurrenceControl from '@/components/userStories/RecurrenceControl'
import Attachments from '@/components/userStories/Attachments'
import TimeTracking from '@/components/userStories/TimeTracking'
import StoryActivity from '@/components/userStories/StoryActivity'
import StoryLinks from '@/components/userStories/StoryLinks'
import MarkdownContent from '@/components/common/MarkdownContent'
import { Skeleton } from '@/components/common/Skeleton'
import { useUserNames, displayNameOrId } from '@/hooks/useUserNames'
import { isOverdue } from '@/lib/dueDate'
import { ticketCode } from '@/lib/ticketCode'
import { useToast } from '@/context/ToastContext'

// A permanent, shareable/bookmarkable view of a single story — the modal
// (still used for quick edits from the Backlog list) doesn't have its own
// URL, so there was previously no way to link someone straight to one story.
export default function StoryDetailPage() {
  const { teamId, storyId } = useParams<{ teamId: string; storyId: string }>()
  const { showToast } = useToast()
  const [team, setTeam] = useState<Team | null>(null)
  const [story, setStory] = useState<UserStory | null>(null)
  const [isLoading, setLoading] = useState(true)
  const [isEditing, setEditing] = useState(false)
  const userNames = useUserNames([...(team?.members.map((m) => m.userId) ?? []), story?.assigneeId])

  const load = () => {
    if (!teamId || !storyId) return
    Promise.all([teamsApi.getById(teamId), userStoriesApi.getById(storyId)])
      .then(([t, s]) => { setTeam(t); setStory(s) })
      .finally(() => setLoading(false))
  }

  useEffect(load, [teamId, storyId])

  const handleStatusChange = async (status: UserStoryStatus) => {
    if (!story) return
    try {
      await userStoriesApi.changeStatus(story.id, status)
      load()
    } catch {
      showToast("Can't move to that status directly from here.", 'error')
    }
  }

  const handlePriorityChange = async (priority: UserStoryPriority) => {
    if (!story) return
    await userStoriesApi.changePriority(story.id, priority)
    load()
  }

  const handleToggleLabel = async (labelId: string) => {
    if (!story) return
    try {
      if (story.labelIds.includes(labelId)) {
        await userStoriesApi.removeLabel(story.id, labelId)
      } else {
        await userStoriesApi.addLabel(story.id, labelId)
      }
      load()
    } catch {
      showToast('Could not update labels on this story.', 'error')
    }
  }

  const handleSaveEdit = async (title: string, description: string, dueDate: string | undefined, storyPoints: number | undefined) => {
    if (!story) return
    try {
      await userStoriesApi.update(story.id, title, description || undefined, dueDate, storyPoints, story.version)
      setEditing(false)
      load()
      showToast('Changes saved.')
    } catch {
      showToast('Could not save — someone else may have edited this story. Refresh and try again.', 'error')
    }
  }

  if (isLoading) {
    return (
      <section>
        <Skeleton className="skeleton-title" />
        <Skeleton style={{ height: 200 }} />
      </section>
    )
  }

  if (!team || !story) {
    return (
      <section>
        <div className="empty-state">
          <div className="empty-state-title">Story not found</div>
          <p>It may have been deleted.</p>
        </div>
      </section>
    )
  }

  return (
    <section>
      <div className="breadcrumb">
        <Link to={`/teams/${team.id}/backlog`}>← Back to {team.name} backlog</Link>
      </div>

      <div className="page-header">
        <div>
          <span className="page-header-eyebrow mono">{ticketCode(team.name, story.id)}</span>
          <h1>{story.recurrenceFrequency && <span title={`Repeats ${story.recurrenceFrequency}`}>🔁 </span>}{story.title}</h1>
        </div>
        <div className="page-header-actions">
          <button className="btn" onClick={() => setEditing(true)}>Edit</button>
        </div>
      </div>

      <div style={{ display: 'flex', gap: 8, marginBottom: 16, flexWrap: 'wrap', alignItems: 'center' }}>
        <select className="pill-select" value={story.status} onChange={(e) => handleStatusChange(e.target.value as UserStoryStatus)}>
          {(['ToDo', 'Analyze', 'Dev', 'Test', 'Debug', 'Done'] as UserStoryStatus[]).map((s) => (
            <option key={s} value={s}>{s === 'ToDo' ? 'To Do' : s}</option>
          ))}
        </select>
        <select className="pill-select" value={story.priority} onChange={(e) => handlePriorityChange(e.target.value as UserStoryPriority)}>
          {(['Critical', 'High', 'Medium', 'Low'] as UserStoryPriority[]).map((p) => (
            <option key={p} value={p}>{p}</option>
          ))}
        </select>
        {story.isArchived && <StatusBadge status={story.status} />}
      </div>

      {story.description && (
        <div className="card">
          <MarkdownContent content={story.description} />
        </div>
      )}

      <div className="card">
        <div className="card-header"><h3>Details</h3></div>
        <div className="dashboard-metric-row"><span className="dashboard-label">Assignee</span><span>{story.assigneeId ? displayNameOrId(userNames, story.assigneeId) : 'Unassigned'}</span></div>
        <div className="dashboard-metric-row"><span className="dashboard-label">Priority</span><PriorityBadge priority={story.priority} /></div>
        <div className="dashboard-metric-row"><span className="dashboard-label">Story points</span><span>{story.storyPoints ?? 'Not estimated'}</span></div>
        <div className="dashboard-metric-row"><span className="dashboard-label">Due date</span><span className={isOverdue(story) ? 'backlog-due-date overdue' : undefined}>{story.dueDate ? new Date(story.dueDate).toLocaleDateString() : 'No due date'}</span></div>
        <div className="dashboard-metric-row"><span className="dashboard-label">Archived</span><span>{story.isArchived ? 'Yes' : 'No'}</span></div>
      </div>

      {team.labels.length > 0 && (
        <div className="card">
          <div className="card-header"><h3>Labels</h3></div>
          <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
            {team.labels.map((label) => {
              const applied = story.labelIds.includes(label.id)
              return (
                <button
                  key={label.id}
                  onClick={() => handleToggleLabel(label.id)}
                  style={{ border: 'none', background: 'none', padding: 0, cursor: 'pointer', opacity: applied ? 1 : 0.4 }}
                  title={applied ? `Remove ${label.name}` : `Apply ${label.name}`}
                >
                  <LabelChip label={label} />
                </button>
              )
            })}
          </div>
        </div>
      )}

      <div className="card">
        <Checklist userStoryId={story.id} items={story.checklistItems} onChange={load} />
      </div>

      <RecurrenceControl story={story} onChange={load} />

      <div className="card">
        <Attachments userStoryId={story.id} attachments={story.attachments} userNames={userNames} onChange={load} />
      </div>

      <TimeTracking story={story} userNames={userNames} onChange={load} />

      <div className="card">
        <CommentSection userStoryId={story.id} members={team.members} userNames={userNames} />
      </div>

      <StoryLinks story={story} teamName={team.name} />

      <StoryActivity userStoryId={story.id} userNames={userNames} />

      <EditUserStoryModal
        story={isEditing ? story : null}
        members={team.members}
        labels={team.labels}
        userNames={userNames}
        onClose={() => setEditing(false)}
        onSave={handleSaveEdit}
        onLabelsChanged={load}
      />
    </section>
  )
}
