import { NavLink } from 'react-router-dom'

interface TeamTabsProps {
  teamId: string
}

const TABS = [
  { path: 'summary', label: 'Summary' },
  { path: 'backlog', label: 'Backlog' },
  { path: 'sprints', label: 'Sprints' },
  { path: 'board', label: 'Board' },
  { path: 'calendar', label: 'Calendar' },
  { path: 'dashboard', label: 'Dashboard' },
  { path: 'activity', label: 'Activity' },
  { path: 'members', label: 'Members' },
  { path: 'archived', label: 'Archived' },
]

export default function TeamTabs({ teamId }: TeamTabsProps) {
  return (
    <nav className="team-tabs">
      {TABS.map((tab) => (
        <NavLink
          key={tab.path}
          to={`/teams/${teamId}/${tab.path}`}
          className={({ isActive }) => `team-tab ${isActive ? 'active' : ''}`}
        >
          {tab.label}
        </NavLink>
      ))}
    </nav>
  )
}
