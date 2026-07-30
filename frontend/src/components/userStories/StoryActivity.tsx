import { useEffect, useState } from 'react'
import type { Activity } from '@/types/activity'
import { userStoriesApi } from '@/api/userStories'
import { displayNameOrId } from '@/hooks/useUserNames'

interface StoryActivityProps {
  userStoryId: string
  userNames: Record<string, string>
}

// US-131: this story's own activity history. Deliberately kept as its own
// separate section rather than interleaved with Comments — field-change
// events (status/assignee/etc.) and discussion are different kinds of
// content, and a reader scanning "what changed" doesn't want to wade through
// conversation to find it (and vice versa).
export default function StoryActivity({ userStoryId, userNames }: StoryActivityProps) {
  const [activities, setActivities] = useState<Activity[]>([])
  const [isLoading, setLoading] = useState(true)

  useEffect(() => {
    userStoriesApi.getActivity(userStoryId).then(setActivities).finally(() => setLoading(false))
  }, [userStoryId])

  return (
    <div className="card">
      <div className="card-header"><h3>Activity</h3></div>
      {isLoading ? (
        <p style={{ fontSize: 13 }}>Loading…</p>
      ) : activities.length === 0 ? (
        <p style={{ fontSize: 13 }}>No activity yet.</p>
      ) : (
        <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
          {activities.map((a) => (
            <li key={a.id} style={{ padding: '6px 0', borderBottom: '1px solid var(--color-border)', fontSize: 13 }}>
              <strong>{displayNameOrId(userNames, a.actorUserId)}</strong> {a.message}
              <div className="mono" style={{ fontSize: 11, color: 'var(--color-ink-faint)' }}>
                {new Date(a.createdOn).toLocaleString()}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
