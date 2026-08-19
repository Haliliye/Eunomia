import { apiClient } from './client'
import type { ImportSummary } from './userStories'
import type { Team } from '@/types/team'

export interface GitHubStatus {
  isConnected: boolean
  gitHubLogin?: string
  connectedOn?: string
}

export interface GitHubRepository {
  owner: string
  name: string
  fullName: string
}

export interface CreateTeamFromGitHubResult {
  team: Team
  importSummary: ImportSummary
}

export const gitHubApi = {
  getStatus: () => apiClient.get<GitHubStatus>('/integrations/github/status').then((res) => res.data),

  // Returns the URL to redirect the whole page to (GitHub's consent screen
  // can't run inside our SPA) — the caller does `window.location.href = authorizationUrl`.
  connect: () =>
    apiClient.get<{ authorizationUrl: string }>('/integrations/github/connect').then((res) => res.data.authorizationUrl),

  disconnect: () => apiClient.delete('/integrations/github/disconnect'),

  getRepositories: () => apiClient.get<GitHubRepository[]>('/integrations/github/repositories').then((res) => res.data),

  importRepo: (owner: string, repo: string, teamId: string) =>
    apiClient.post<ImportSummary>(`/integrations/github/repos/${encodeURIComponent(owner)}/${encodeURIComponent(repo)}/import`, null, { params: { teamId } }).then((res) => res.data),

  createTeamFromRepo: (owner: string, repo: string, teamName?: string) =>
    apiClient.post<CreateTeamFromGitHubResult>(`/integrations/github/repos/${encodeURIComponent(owner)}/${encodeURIComponent(repo)}/create-team`, { teamName }).then((res) => res.data),
}
