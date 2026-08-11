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

export const azureDevOpsApi = {
  getStatus: () => apiClient.get<AzureDevOpsStatus>('/integrations/azuredevops/status').then((res) => res.data),

  // Returns the URL to redirect the whole page to (Microsoft's sign-in/
  // consent screen can't run inside our SPA) — the caller does
  // `window.location.href = authorizationUrl`.
  connect: () =>
    apiClient.get<{ authorizationUrl: string }>('/integrations/azuredevops/connect').then((res) => res.data.authorizationUrl),

  disconnect: () => apiClient.delete('/integrations/azuredevops/disconnect'),

  getOrganizations: () => apiClient.get<string[]>('/integrations/azuredevops/organizations').then((res) => res.data),

  setOrganization: (organizationName: string) =>
    apiClient.put('/integrations/azuredevops/organization', { organizationName }),

  getProjects: () => apiClient.get<AzureDevOpsProject[]>('/integrations/azuredevops/projects').then((res) => res.data),

  importProject: (projectName: string, teamId: string) =>
    apiClient.post<ImportSummary>(`/integrations/azuredevops/projects/${encodeURIComponent(projectName)}/import`, null, { params: { teamId } }).then((res) => res.data),

  createTeamFromProject: (projectName: string, teamName?: string) =>
    apiClient.post<CreateTeamFromAzureDevOpsResult>(`/integrations/azuredevops/projects/${encodeURIComponent(projectName)}/create-team`, { teamName }).then((res) => res.data),
}
