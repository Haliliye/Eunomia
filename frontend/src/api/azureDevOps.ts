import { apiClient } from './client'
import type { ImportSummary } from './userStories'
import type { Team } from '@/types/team'

export interface AzureDevOpsStatus {
  isConnected: boolean
  organizationName?: string
  connectedOn?: string
}

export interface AzureDevOpsProject {
  id: string
  name: string
}

export interface CreateTeamFromAzureDevOpsResult {
  team: Team
  importSummary: ImportSummary
}

export interface AzureDevOpsSyncStatus {
  isLinked: boolean
  projectName?: string
  autoSyncEnabled: boolean
  lastSyncedOn?: string
}

export const azureDevOpsApi = {
  getStatus: () => apiClient.get<AzureDevOpsStatus>('/integrations/azuredevops/status').then((res) => res.data),

  // PAT-based — no redirect, just posts the organization name + token.
  connect: (organizationName: string, personalAccessToken: string) =>
    apiClient.post<{ success: boolean; errorMessage?: string }>('/integrations/azuredevops/connect', { organizationName, personalAccessToken }).then((res) => res.data),

  disconnect: () => apiClient.delete('/integrations/azuredevops/disconnect'),

  getProjects: () => apiClient.get<AzureDevOpsProject[]>('/integrations/azuredevops/projects').then((res) => res.data),

  importProject: (projectName: string, teamId: string, setAutoSync?: boolean) =>
    apiClient.post<ImportSummary>(`/integrations/azuredevops/projects/${encodeURIComponent(projectName)}/import`, null, { params: { teamId, setAutoSync } }).then((res) => res.data),

  createTeamFromProject: (projectName: string, teamName?: string, setAutoSync?: boolean) =>
    apiClient.post<CreateTeamFromAzureDevOpsResult>(`/integrations/azuredevops/projects/${encodeURIComponent(projectName)}/create-team`, { teamName, setAutoSync }).then((res) => res.data),

  getSyncStatus: (teamId: string) =>
    apiClient.get<AzureDevOpsSyncStatus>(`/integrations/azuredevops/teams/${teamId}/sync-status`).then((res) => res.data),

  setAutoSync: (teamId: string, enabled: boolean) =>
    apiClient.put(`/integrations/azuredevops/teams/${teamId}/auto-sync`, { enabled }),

  syncTeamNow: (teamId: string) =>
    apiClient.post<ImportSummary>(`/integrations/azuredevops/teams/${teamId}/sync-now`).then((res) => res.data),
}
