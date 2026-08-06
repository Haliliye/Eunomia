import { useState } from 'react'
import type { ReactNode } from 'react'

interface StoryActivityTabsProps {
  comments: ReactNode
  history: ReactNode
  worklog: ReactNode
}

type Tab = 'all' | 'comments' | 'history' | 'worklog'

const TABS: { id: Tab; label: string }[] = [
  { id: 'all', label: 'All' },
  { id: 'comments', label: 'Comments' },
  { id: 'history', label: 'History' },
  { id: 'worklog', label: 'Worklog' },
]

// A lightweight switcher, not a merged timeline — "All" just stacks the three
// sections the way this page already did before tabs existed, so nothing
// about what each section shows had to change, only how they're navigated.
export default function StoryActivityTabs({ comments, history, worklog }: StoryActivityTabsProps) {
  const [tab, setTab] = useState<Tab>('all')

  return (
    <div>
      <nav className="team-tabs" style={{ marginBottom: 'var(--space-4)' }}>
        {TABS.map((t) => (
          <button
            key={t.id}
            className={`team-tab ${tab === t.id ? 'active' : ''}`}
            style={{ background: 'none', border: 'none', borderBottom: '2px solid transparent', cursor: 'pointer' }}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </nav>

      {(tab === 'all' || tab === 'comments') && comments}
      {(tab === 'all' || tab === 'history') && history}
      {(tab === 'all' || tab === 'worklog') && worklog}
    </div>
  )
}
