import { integrationsApi } from '@/api/integrations'
import { azureDevOpsApi } from '@/api/azureDevOps'
import { gitHubApi } from '@/api/github'
import { gitLabApi } from '@/api/gitlab'
import type { Team } from '@/types/team'
import type { ImportRow, ImportSummary } from '@/api/userStories'

export interface ImportableItem {
  /** Stable, unique-enough-to-round-trip identifier for this item — encodes whatever the provider's createTeam/importIntoTeam calls actually need (see each adapter below), not just a display id. */
  key: string
  label: string
}

export interface CreateTeamResult {
  team: Team
  importSummary: ImportSummary
}

/**
 * One shape for "pick a project/repo, then either create a new team from it
 * or import it into an existing one" across all four source-control/
 * issue-tracker integrations — this used to be eight separate near-
 * duplicate modals (four CreateTeamFrom*Modal, four *ImportModal). One
 * ImportTeamModal drives the whole flow off whichever adapter is picked;
 * only the adapter changes per provider.
 */
export interface ImportProviderAdapter {
  id: 'jira' | 'azure-devops' | 'github' | 'gitlab'
  displayName: string
  /** "project" or "repo" — used in copy like "Pick a Jira project". */
  itemNoun: string
  getIsConnected: () => Promise<boolean>
  listItems: () => Promise<ImportableItem[]>
  defaultTeamNameFor: (item: ImportableItem) => string
  noItemsMessage: string
  infoText: (item: ImportableItem) => string
  supportsAutoSync: boolean
  createTeam: (item: ImportableItem, teamName: string, autoSync: boolean) => Promise<CreateTeamResult>
  /** Only Jira supports a preview-before-confirming step — undefined for the other three. */
  previewImport?: (item: ImportableItem) => Promise<ImportRow[]>
  importIntoTeam: (item: ImportableItem, teamId: string, autoSync: boolean) => Promise<ImportSummary>
}

export const jiraAdapter: ImportProviderAdapter = {
  id: 'jira',
  displayName: 'Jira',
  itemNoun: 'project',
  getIsConnected: () => integrationsApi.getJiraStatus().then((s) => s.isConnected),
  listItems: () => integrationsApi.getJiraProjects().then((list) =>
    list.map((p) => ({ key: p.key, label: `${p.key}  ${p.name}` }))),
  defaultTeamNameFor: (item) => item.label.replace(/^\S+\s+/, ''), // strips the "KEY  " prefix back off
  noItemsMessage: 'No projects found on this Jira site.',
  infoText: (item) =>
    `Every issue in ${item.label} will be imported, along with labels, story points, comments, attachments, sprints, and issue links. The new team's board gets one column per distinct Jira status. Assignees with a matching Eunomia account are assigned automatically; others get an email invitation to join and are added to this team once they sign up.`,
  supportsAutoSync: true,
  createTeam: (item, teamName, autoSync) => integrationsApi.createTeamFromJira(item.key, teamName, autoSync),
  previewImport: (item) => integrationsApi.previewJiraImport(item.key),
  importIntoTeam: (item, teamId, autoSync) => integrationsApi.importJiraProject(item.key, teamId, autoSync),
}

export const azureDevOpsAdapter: ImportProviderAdapter = {
  id: 'azure-devops',
  displayName: 'Azure DevOps',
  itemNoun: 'project',
  getIsConnected: () => azureDevOpsApi.getStatus().then((s) => s.isConnected),
  listItems: () => azureDevOpsApi.getProjects().then((list) =>
    list.map((p) => ({ key: p.name, label: p.name }))),
  defaultTeamNameFor: (item) => item.label,
  noItemsMessage: 'No projects found in this organization.',
  infoText: (item) =>
    `Every work item in ${item.label} will be imported, along with tags, story points, comments, attachments, iterations, and work item links. The new team's board gets one column per distinct work item state. Assignees with a matching Eunomia account are assigned automatically; others get an email invitation to join and are added to this team once they sign up.`,
  supportsAutoSync: true,
  createTeam: (item, teamName, autoSync) => azureDevOpsApi.createTeamFromProject(item.key, teamName, autoSync),
  importIntoTeam: (item, teamId, autoSync) => azureDevOpsApi.importProject(item.key, teamId, autoSync),
}

export const gitHubAdapter: ImportProviderAdapter = {
  id: 'github',
  displayName: 'GitHub',
  itemNoun: 'repo',
  getIsConnected: () => gitHubApi.getStatus().then((s) => s.isConnected),
  listItems: () => gitHubApi.getRepositories().then((list) =>
    list.map((r) => ({ key: `${r.owner}/${r.name}`, label: r.fullName }))),
  defaultTeamNameFor: (item) => item.key.split('/')[1] ?? item.label,
  noItemsMessage: 'No repositories found for this GitHub account.',
  infoText: (item) =>
    `Open issues in ${item.label} will be imported, along with labels and comments — closed issues land in Done, everything else in To Do. Assignees with a public GitHub email and a matching Eunomia account are assigned automatically. Pull requests aren't imported.`,
  supportsAutoSync: false,
  createTeam: (item, teamName) => {
    const [owner, repo] = item.key.split('/')
    return gitHubApi.createTeamFromRepo(owner, repo, teamName)
  },
  importIntoTeam: (item, teamId) => {
    const [owner, repo] = item.key.split('/')
    return gitHubApi.importRepo(owner, repo, teamId)
  },
}

export const gitLabAdapter: ImportProviderAdapter = {
  id: 'gitlab',
  displayName: 'GitLab',
  itemNoun: 'project',
  getIsConnected: () => gitLabApi.getStatus().then((s) => s.isConnected),
  listItems: () => gitLabApi.getProjects().then((list) =>
    list.map((p) => ({ key: `${p.id}|${p.pathWithNamespace}|${p.name}`, label: p.pathWithNamespace }))),
  defaultTeamNameFor: (item) => item.key.split('|')[2] ?? item.label,
  noItemsMessage: 'No projects found for this GitLab account.',
  infoText: (item) =>
    `Open issues in ${item.label} will be imported, along with labels and notes (comments) — closed issues land in Done, everything else in To Do. Assignees with a public GitLab email and a matching Eunomia account are assigned automatically.`,
  supportsAutoSync: false,
  createTeam: (item, teamName) => {
    const [idStr, pathWithNamespace, projectName] = item.key.split('|')
    return gitLabApi.createTeamFromProject(Number(idStr), pathWithNamespace, projectName, teamName)
  },
  importIntoTeam: (item, teamId) => {
    const [idStr, pathWithNamespace] = item.key.split('|')
    return gitLabApi.importProject(Number(idStr), pathWithNamespace, teamId)
  },
}

export const importProviders: ImportProviderAdapter[] = [jiraAdapter, azureDevOpsAdapter, gitHubAdapter, gitLabAdapter]
