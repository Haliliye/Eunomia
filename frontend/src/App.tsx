import { Suspense, lazy } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import Layout from './components/common/Layout'
import ProtectedRoute from './components/common/ProtectedRoute'
import { Skeleton } from './components/common/Skeleton'

// Route-level code splitting — each page becomes its own chunk, loaded on
// first visit instead of all 21 pages shipping in one ~560KB bundle up
// front. Auth pages (Login/Register/etc.) stay eager: they're the very
// first thing an unauthenticated visitor sees, so lazy-loading them would
// trade a bundle-size win for a slower first paint on the page that matters
// most for that. Everything behind ProtectedRoute — the actual app — is lazy.
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import ForgotPasswordPage from './pages/ForgotPasswordPage'
import ResetPasswordPage from './pages/ResetPasswordPage'
import VerifyEmailPage from './pages/VerifyEmailPage'

const TeamsPage = lazy(() => import('./pages/TeamsPage'))
const PortfolioPage = lazy(() => import('./pages/PortfolioPage'))
const TeamShellPage = lazy(() => import('./pages/TeamShellPage'))
const TeamSummaryPage = lazy(() => import('./pages/TeamSummaryPage'))
const TeamBacklogPage = lazy(() => import('./pages/TeamBacklogPage'))
const TeamSprintsPage = lazy(() => import('./pages/TeamSprintsPage'))
const TeamMembersPage = lazy(() => import('./pages/TeamMembersPage'))
const TeamArchivedPage = lazy(() => import('./pages/TeamArchivedPage'))
const BoardPage = lazy(() => import('./pages/BoardPage'))
const DashboardPage = lazy(() => import('./pages/DashboardPage'))
const TeamActivityPage = lazy(() => import('./pages/TeamActivityPage'))
const CalendarPage = lazy(() => import('./pages/CalendarPage'))
const StoryDetailPage = lazy(() => import('./pages/StoryDetailPage'))
const SettingsPage = lazy(() => import('./pages/SettingsPage'))
const MyWorkPage = lazy(() => import('./pages/MyWorkPage'))
const MyTasksPage = lazy(() => import('./pages/MyTasksPage'))

function PageFallback() {
  return (
    <section style={{ padding: 'var(--space-6)' }}>
      <Skeleton className="skeleton-title" />
      <Skeleton style={{ height: 32, marginBottom: 16 }} />
    </section>
  )
}

export default function App() {
  return (
    <Suspense fallback={<PageFallback />}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/verify-email" element={<VerifyEmailPage />} />

        <Route element={<ProtectedRoute />}>
          <Route element={<Layout />}>
            <Route path="/" element={<Navigate to="/teams" replace />} />
            <Route path="/teams" element={<TeamsPage />} />
            <Route path="/portfolio" element={<PortfolioPage />} />

            <Route path="/teams/:teamId" element={<TeamShellPage />}>
              <Route index element={<Navigate to="summary" replace />} />
              <Route path="summary" element={<TeamSummaryPage />} />
              <Route path="backlog" element={<TeamBacklogPage />} />
              <Route path="sprints" element={<TeamSprintsPage />} />
              <Route path="board" element={<BoardPage />} />
              <Route path="calendar" element={<CalendarPage />} />
              <Route path="dashboard" element={<DashboardPage />} />
              <Route path="activity" element={<TeamActivityPage />} />
              <Route path="members" element={<TeamMembersPage />} />
              <Route path="archived" element={<TeamArchivedPage />} />
            </Route>
            <Route path="/teams/:teamId/stories/:storyId" element={<StoryDetailPage />} />
            <Route path="/settings" element={<SettingsPage />} />
            <Route path="/my-work" element={<MyWorkPage />} />
            <Route path="/my-tasks" element={<MyTasksPage />} />
          </Route>
        </Route>
      </Routes>
    </Suspense>
  )
}
