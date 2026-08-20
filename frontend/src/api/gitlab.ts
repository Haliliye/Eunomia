import { apiClient } from './client'
import type { ImportSummary } from './userStories'
import type { Team } from '@/types/team'

export interface GitLabStatus {
  isConnected: boolean
  gitLabUsername?: string
  connectedOn?: string
}

export interface GitLabProject {
  id: number
  name: string
  pathWithNamespace: string
}

export interface CreateTeamFromGitLabResult {
  team: Team
  importSummary: ImportSummary
}

export const gitLabApi = {
  getStatus: () => apiClient.get<GitLabStatus>('/integrations/gitlab/status').then((res) => res.data),

  // Returns the URL to redirect the whole page to (GitLab's consent screen
  // can't run inside our SPA) — the caller does `window.location.href = authorizationUrl`.
  connect: () =>
    apiClient.get<{ authorizationUrl: string }>('/integrations/gitlab/connect').then((res) => res.data.authorizationUrl),

  disconnect: () => apiClient.delete('/integrations/gitlab/disconnect'),

  getProjects: () => apiClient.get<GitLabProject[]>('/integrations/gitlab/projects').then((res) => res.data),

  importProject: (projectId: number, pathWithNamespace: string, teamId: string) =>
    apiClient.post<ImportSummary>(`/integrations/gitlab/projects/${projectId}/import`, null, { params: { teamId, pathWithNamespace } }).then((res) => res.data),

  createTeamFromProject: (projectId: number, pathWithNamespace: string, projectName: string, teamName?: string) =>
    apiClient.post<CreateTeamFromGitLabResult>(`/integrations/gitlab/projects/${projectId}/create-team`, { pathWithNamespace, projectName, teamName }).then((res) => res.data),
}
