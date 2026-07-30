import { Routes, Route, Navigate } from 'react-router-dom'
import Layout from './components/common/Layout'
import ProtectedRoute from './components/common/ProtectedRoute'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import ForgotPasswordPage from './pages/ForgotPasswordPage'
import ResetPasswordPage from './pages/ResetPasswordPage'
import VerifyEmailPage from './pages/VerifyEmailPage'
import TeamsPage from './pages/TeamsPage'
import TeamShellPage from './pages/TeamShellPage'
import TeamSummaryPage from './pages/TeamSummaryPage'
import TeamBacklogPage from './pages/TeamBacklogPage'
import TeamSprintsPage from './pages/TeamSprintsPage'
import TeamMembersPage from './pages/TeamMembersPage'
import TeamArchivedPage from './pages/TeamArchivedPage'
import BoardPage from './pages/BoardPage'
import DashboardPage from './pages/DashboardPage'
import TeamActivityPage from './pages/TeamActivityPage'
import CalendarPage from './pages/CalendarPage'
import StoryDetailPage from './pages/StoryDetailPage'
import SettingsPage from './pages/SettingsPage'
import MyWorkPage from './pages/MyWorkPage'
import MyTasksPage from './pages/MyTasksPage'

export default function App() {
  return (
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
  )
}
