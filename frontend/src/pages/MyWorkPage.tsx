import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { personalTasksApi } from '@/api/personalTasks'
import { userStoriesApi } from '@/api/userStories'
import type { MyWorkItem } from '@/types/personalTask'
import { useToast } from '@/context/ToastContext'
import { SkeletonTable } from '@/components/common/Skeleton'

// US-142: personal tasks + team stories assigned to me, in one place.
// Grouped into its own section per source (Personal, then one per team)
// rather than a single mixed list — makes it obvious at a glance what's
// personal vs. what belongs to which team.
export default function MyWorkPage() {
  const { showToast } = useToast()
  const [items, setItems] = useState<MyWorkItem[]>([])
  const [isLoading, setLoading] = useState(true)

  const load = () => {
    personalTasksApi.getMyWork().then(setItems).finally(() => setLoading(false))
  }

  useEffect(load, [])

  const handleToggle = async (item: MyWorkItem) => {
    try {
      if (item.sourceType === 'Personal') {
        await personalTasksApi.toggle(item.id, !item.isCompleted)
      } else {
        // A team story only has a real "Done" state, not a boolean toggle —
        // reopening from here sends it back to ToDo (a reasonable default
        // even though a Done->ToDo jump isn't normally offered on the board itself).
        await userStoriesApi.changeStatus(item.id, item.isCompleted ? 'ToDo' : 'Done')
      }
      load()
    } catch {
      showToast('Could not update that item.', 'error')
    }
  }

  const groups = useMemo(() => {
    const byKey = new Map<string, { label: string; items: MyWorkItem[] }>()

    for (const item of items) {
      const key = item.sourceType === 'Personal' ? 'personal' : `team:${item.teamId}`
      const label = item.sourceType === 'Personal' ? 'Personal' : (item.teamName ?? 'Unknown team')
      const group = byKey.get(key) ?? { label, items: [] }
      group.items.push(item)
      byKey.set(key, group)
    }

    for (const group of byKey.values()) {
      group.items.sort((a, b) => {
        if (a.isCompleted !== b.isCompleted) return a.isCompleted ? 1 : -1
        const aDate = a.dueDate ? new Date(a.dueDate).getTime() : Infinity
        const bDate = b.dueDate ? new Date(b.dueDate).getTime() : Infinity
        return aDate - bDate
      })
    }

    // Personal first, then teams alphabetically by name.
    return Array.from(byKey.entries())
      .sort(([keyA, a], [keyB, b]) => {
        if (keyA === 'personal') return -1
        if (keyB === 'personal') return 1
        return a.label.localeCompare(b.label)
      })
      .map(([key, group]) => ({ key, ...group }))
  }, [items])

  return (
    <section>
      <div className="page-header">
        <div>
          <span className="page-header-eyebrow">Overview</span>
          <h1>My Work</h1>
          <p>Your personal tasks and everything assigned to you across all teams.</p>
        </div>
      </div>

      {isLoading ? (
        <SkeletonTable />
      ) : items.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-title">Nothing here yet</div>
          <p>Personal tasks and stories assigned to you will show up here.</p>
        </div>
      ) : (
        groups.map((group) => (
          <div className="card" key={group.key}>
            <div className="card-header">
              <h3>{group.label}</h3>
              <span className="mono" style={{ fontSize: 12.5, color: 'var(--color-ink-muted)' }}>
                {group.items.filter((i) => !i.isCompleted).length} open
              </span>
            </div>
            <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
              {group.items.map((item) => (
                <li key={item.id} className="member-row">
                  <span style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <input type="checkbox" checked={item.isCompleted} onChange={() => handleToggle(item)} />
                    {item.sourceType === 'TeamStory' && item.teamId ? (
                      <Link to={`/teams/${item.teamId}/stories/${item.id}`} style={{ textDecoration: item.isCompleted ? 'line-through' : undefined }}>
                        {item.title}
                      </Link>
                    ) : (
                      <span style={{ textDecoration: item.isCompleted ? 'line-through' : undefined, color: item.isCompleted ? 'var(--color-ink-faint)' : undefined }}>
                        {item.title}
                      </span>
                    )}
                  </span>
                  {item.dueDate && <span className="mono" style={{ fontSize: 11, color: 'var(--color-ink-faint)' }}>{new Date(item.dueDate).toLocaleDateString()}</span>}
                </li>
              ))}
            </ul>
          </div>
        ))
      )}
    </section>
  )
}
