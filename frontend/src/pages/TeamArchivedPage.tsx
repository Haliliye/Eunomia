import { useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { userStoriesApi } from '@/api/userStories'
import type { UserStory } from '@/types/userStory'
import { ticketCode } from '@/lib/ticketCode'
import PriorityBadge from '@/components/common/PriorityBadge'
import { SkeletonTable } from '@/components/common/Skeleton'
import { useToast } from '@/context/ToastContext'
import type { TeamOutletContext } from './TeamShellPage'

export default function TeamArchivedPage() {
  const { team } = useOutletContext<TeamOutletContext>()
  const { showToast } = useToast()
  const [stories, setStories] = useState<UserStory[]>([])
  const [isLoading, setLoading] = useState(true)

  const load = () => {
    setLoading(true)
    userStoriesApi.getByTeam(team.id, {}, 1, 100, true)
      .then((result) => setStories(result.items))
      .finally(() => setLoading(false))
  }

  useEffect(load, [team.id])

  const handleUnarchive = async (story: UserStory) => {
    try {
      await userStoriesApi.unarchive(story.id)
      setStories((prev) => prev.filter((s) => s.id !== story.id))
      showToast(`"${story.title}" was restored to the backlog.`)
    } catch {
      showToast('Could not restore that story.', 'error')
    }
  }

  if (isLoading) return <SkeletonTable />

  if (stories.length === 0) {
    return (
      <div className="empty-state">
        <div className="empty-state-title">No archived stories</div>
        <p>Stories you archive from the Backlog tab show up here.</p>
      </div>
    )
  }

  return (
    <div className="backlog-list">
      {stories.map((story) => (
        <div className="backlog-row" key={story.id}>
          <span className="backlog-key">{ticketCode(team.name, story.id)}</span>
          <span className="backlog-title">{story.title}</span>
          <PriorityBadge priority={story.priority} />
          <div className="backlog-row-actions" style={{ opacity: 1 }}>
            <button className="btn btn-sm" onClick={() => handleUnarchive(story)}>Unarchive</button>
          </div>
        </div>
      ))}
    </div>
  )
}
