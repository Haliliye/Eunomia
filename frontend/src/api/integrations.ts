import { apiClient } from './client'
import type { ImportRow, ImportSummary } from './userStories'
import type { Team } from '@/types/team'

export interface JiraStatus {
  isConnected: boolean
  siteName?: string
  connectedOn?: string
}

export interface JiraProject {
  key: string
  name: string
  avatarUrl?: string
}

export interface CreateTeamFromJiraResult {
  team: Team
  importSummary: ImportSummary
}

export const integrationsApi = {
  getJiraStatus: () => apiClient.get<JiraStatus>('/integrations/jira/status').then((res) => res.data),

  // Returns the URL to redirect the whole page to (Atlassian's consent
  // screen can't run inside our SPA) — the caller does `window.location.href = authorizationUrl`.
  connectJira: () =>
    apiClient.get<{ authorizationUrl: string }>('/integrations/jira/connect').then((res) => res.data.authorizationUrl),

  disconnectJira: () => apiClient.delete('/integrations/jira/disconnect'),

  getJiraProjects: () => apiClient.get<JiraProject[]>('/integrations/jira/projects').then((res) => res.data),

  previewJiraImport: (projectKey: string) =>
    apiClient.get<ImportRow[]>(`/integrations/jira/projects/${projectKey}/preview`).then((res) => res.data),

  importJiraProject: (projectKey: string, teamId: string) =>
    apiClient.post<ImportSummary>(`/integrations/jira/projects/${projectKey}/import`, null, { params: { teamId } }).then((res) => res.data),

  createTeamFromJira: (projectKey: string, teamName?: string) =>
    apiClient.post<CreateTeamFromJiraResult>(`/integrations/jira/projects/${projectKey}/create-team`, { teamName }).then((res) => res.data),
}
