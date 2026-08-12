import { useEffect, useMemo, useState } from 'react'
import { useOutletContext, Link } from 'react-router-dom'
import { userStoriesApi } from '@/api/userStories'
import { sprintsApi } from '@/api/sprints'
import type { UserStory } from '@/types/userStory'
import type { Sprint } from '@/types/sprint'
import { isOverdue } from '@/lib/dueDate'
import { ticketCode } from '@/lib/ticketCode'
import { Skeleton } from '@/components/common/Skeleton'
import { useEscapeToClose } from '@/hooks/useEscapeToClose'
import { useFocusTrap } from '@/hooks/useFocusTrap'
import type { TeamOutletContext } from './TeamShellPage'

const WEEKDAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']

function startOfMonthGrid(year: number, month: number): Date {
  const first = new Date(year, month, 1)
  const weekday = (first.getDay() + 6) % 7 // Monday = 0
  const start = new Date(first)
  start.setDate(first.getDate() - weekday)
  return start
}

function isSameDay(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate()
}

// Team-level calendar view — built entirely from data the app already has
// (each story's own due date, plus the active sprint's date range). No
// external calendar account or sync involved; that's a separate, larger
// feature (see README's Sprint 12 notes on Slack/Calendar integration).
export default function CalendarPage() {
  const { team } = useOutletContext<TeamOutletContext>()
  const [cursor, setCursor] = useState(() => { const d = new Date(); d.setDate(1); return d })
  const [stories, setStories] = useState<UserStory[]>([])
  const [activeSprint, setActiveSprint] = useState<Sprint | null>(null)
  const [isLoading, setLoading] = useState(true)
  const [selectedDay, setSelectedDay] = useState<Date | null>(null)
  useEscapeToClose(selectedDay !== null, () => setSelectedDay(null))
  const dayModalRef = useFocusTrap(selectedDay !== null)

  useEffect(() => {
    setLoading(true)
    Promise.all([
      userStoriesApi.getByTeam(team.id, {}, 1, 500, false),
      sprintsApi.getForTeam(team.id),
    ]).then(([result, sprints]) => {
      setStories(result.items.filter((s) => s.dueDate))
      setActiveSprint(sprints.find((s) => s.status === 'Active') ?? null)
    }).finally(() => setLoading(false))
  }, [team.id])

  const year = cursor.getFullYear()
  const month = cursor.getMonth()
  const gridStart = useMemo(() => startOfMonthGrid(year, month), [year, month])

  const days = useMemo(() => {
    return Array.from({ length: 42 }, (_, i) => {
      const d = new Date(gridStart)
      d.setDate(gridStart.getDate() + i)
      return d
    })
  }, [gridStart])

  const storiesByDay = useMemo(() => {
    const map = new Map<string, UserStory[]>()
    for (const story of stories) {
      const key = new Date(story.dueDate!).toDateString()
      const list = map.get(key) ?? []
      list.push(story)
      map.set(key, list)
    }
    return map
  }, [stories])

  const inSprintRange = (day: Date) => {
    if (!activeSprint) return false
    const start = new Date(activeSprint.startDate)
    const end = new Date(activeSprint.endDate)
    return day >= new Date(start.getFullYear(), start.getMonth(), start.getDate())
        && day <= new Date(end.getFullYear(), end.getMonth(), end.getDate())
  }

  const today = new Date()
  const selectedStories = selectedDay ? storiesByDay.get(selectedDay.toDateString()) ?? [] : []

  return (
    <section>
      <div className="page-header">
        <div>
          <span className="page-header-eyebrow">{team.name}</span>
          <h1>Calendar</h1>
        </div>
        <div className="page-header-actions" style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <button className="btn btn-sm" onClick={() => setCursor(new Date(year, month - 1, 1))}>← Prev</button>
          <span style={{ minWidth: 130, textAlign: 'center', fontWeight: 600 }}>
            {cursor.toLocaleDateString(undefined, { month: 'long', year: 'numeric' })}
          </span>
          <button className="btn btn-sm" onClick={() => setCursor(new Date(year, month + 1, 1))}>Next →</button>
          <button className="btn btn-sm" onClick={() => setCursor(new Date(today.getFullYear(), today.getMonth(), 1))}>Today</button>
        </div>
      </div>

      {activeSprint && (
        <p style={{ fontSize: 12.5, color: 'var(--color-ink-muted)', marginBottom: 8 }}>
          <span style={{ display: 'inline-block', width: 10, height: 10, borderRadius: 3, background: 'var(--color-brand-soft)', border: '1px solid var(--color-brand)', marginRight: 6, verticalAlign: 'middle' }} />
          Shaded days fall within the active sprint, <strong>{activeSprint.name}</strong> ({new Date(activeSprint.startDate).toLocaleDateString()} – {new Date(activeSprint.endDate).toLocaleDateString()}).
        </p>
      )}

      {isLoading ? (
        <Skeleton style={{ height: 480 }} />
      ) : (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)', borderBottom: '1px solid var(--color-border)' }}>
            {WEEKDAYS.map((w) => (
              <div key={w} className="mono" style={{ padding: '8px 6px', fontSize: 11, color: 'var(--color-ink-faint)', textAlign: 'center' }}>{w}</div>
            ))}
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)' }}>
            {days.map((day) => {
              const dayStories = storiesByDay.get(day.toDateString()) ?? []
              const isCurrentMonth = day.getMonth() === month
              const isToday = isSameDay(day, today)

              return (
                <div
                  key={day.toISOString()}
                  onClick={() => setSelectedDay(day)}
                  style={{
                    minHeight: 92,
                    padding: 6,
                    borderRight: '1px solid var(--color-border)',
                    borderBottom: '1px solid var(--color-border)',
                    background: inSprintRange(day) ? 'var(--color-brand-soft)' : undefined,
                    opacity: isCurrentMonth ? 1 : 0.4,
                    cursor: 'pointer',
                  }}
                >
                  <div className="mono" style={{ fontSize: 11, fontWeight: isToday ? 700 : 400, color: isToday ? 'var(--color-brand)' : 'var(--color-ink-muted)' }}>
                    {day.getDate()}
                  </div>
                  {dayStories.slice(0, 3).map((story) => (
                    <div
                      key={story.id}
                      title={story.title}
                      style={{
                        fontSize: 11,
                        marginTop: 3,
                        padding: '1px 4px',
                        borderRadius: 4,
                        whiteSpace: 'nowrap',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        background: isOverdue(story) ? 'var(--color-danger-bg)' : 'var(--color-surface-sunken)',
                        color: isOverdue(story) ? 'var(--color-danger)' : 'var(--color-ink)',
                      }}
                    >
                      {story.title}
                    </div>
                  ))}
                  {dayStories.length > 3 && (
                    <div className="mono" style={{ fontSize: 10.5, color: 'var(--color-ink-faint)', marginTop: 2 }}>
                      +{dayStories.length - 3} more
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        </div>
      )}

      {selectedDay && (
        <div className="modal-overlay" onClick={() => setSelectedDay(null)}>
          <div className="modal" ref={dayModalRef} role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
            <h2>{selectedDay.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' })}</h2>
            {selectedStories.length === 0 ? (
              <p style={{ fontSize: 13 }}>Nothing due this day.</p>
            ) : (
              <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
                {selectedStories.map((story) => (
                  <li key={story.id} style={{ padding: '8px 0', borderBottom: '1px solid var(--color-border)' }}>
                    <Link to={`/teams/${team.id}/stories/${story.id}`} className="mono" style={{ fontSize: 11, color: 'var(--color-ink-faint)', display: 'block' }}>
                      {ticketCode(team.name, story.id)}
                    </Link>
                    <Link to={`/teams/${team.id}/stories/${story.id}`}>{story.title}</Link>
                  </li>
                ))}
              </ul>
            )}
            <div className="modal-actions" style={{ marginTop: 16 }}>
              <button className="btn" onClick={() => setSelectedDay(null)}>Close</button>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}
