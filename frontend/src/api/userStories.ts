import { apiClient } from './client'
import type { UserStory, ChecklistItem, Attachment, TimeLogEntry } from '@/types/userStory'
import type { Activity } from '@/types/activity'
import type { PagedResult } from '@/types/paged'

export interface UserStoryFilters {
  status?: string
  priority?: string
  assigneeId?: string
  keyword?: string
}

export interface TeamDashboard {
  countsByStatus: Record<string, number>
  countsByAssignee: Record<string, number>
  totalCount: number
}

export const userStoriesApi = {
  getByTeam: (teamId: string, filters: UserStoryFilters = {}, page = 1, pageSize = 25, showArchived = false, sprintId?: string, labelId?: string) =>
    apiClient.get<PagedResult<UserStory>>('/userstories', { params: { teamId, ...filters, page, pageSize, showArchived, sprintId, labelId } })
      .then((res) => res.data),

  getDashboard: (teamId: string, sprintId?: string) =>
    apiClient.get<TeamDashboard>('/userstories/dashboard', { params: { teamId, sprintId } }).then((res) => res.data),

  getById: (id: string) =>
    apiClient.get<UserStory>(`/userstories/${id}`).then((res) => res.data),

  create: (teamId: string, title: string, description: string | undefined) =>
    apiClient.post<UserStory>('/userstories', { teamId, title, description }).then((res) => res.data),

  bulkCreate: (teamId: string, titles: string[]) =>
    apiClient.post<UserStory[]>('/userstories/bulk', { teamId, titles }).then((res) => res.data),

  update: (id: string, title: string, description: string | undefined, dueDate: string | undefined, storyPoints: number | undefined, expectedVersion: number) =>
    apiClient.put(`/userstories/${id}`, { title, description, dueDate, storyPoints, expectedVersion }),

  delete: (id: string) =>
    apiClient.delete(`/userstories/${id}`),

  changeStatus: (id: string, status: UserStory['status']) =>
    apiClient.put(`/userstories/${id}/status`, { status }),

  changePriority: (id: string, priority: UserStory['priority']) =>
    apiClient.put(`/userstories/${id}/priority`, { priority }),

  assign: (id: string, assigneeId: string | null) =>
    apiClient.put(`/userstories/${id}/assignee`, { assigneeId }),

  archive: (id: string) =>
    apiClient.put(`/userstories/${id}/archive`),

  unarchive: (id: string) =>
    apiClient.put(`/userstories/${id}/unarchive`),

  moveToSprint: (id: string, sprintId: string | null) =>
    apiClient.put(`/userstories/${id}/sprint`, { sprintId }),

  addChecklistItem: (id: string, text: string) =>
    apiClient.post<ChecklistItem>(`/userstories/${id}/checklist-items`, { text }).then((res) => res.data),

  toggleChecklistItem: (id: string, itemId: string) =>
    apiClient.put(`/userstories/${id}/checklist-items/${itemId}/toggle`),

  removeChecklistItem: (id: string, itemId: string) =>
    apiClient.delete(`/userstories/${id}/checklist-items/${itemId}`),

  reorderChecklistItems: (id: string, orderedItemIds: string[]) =>
    apiClient.put(`/userstories/${id}/checklist-items/reorder`, { orderedItemIds }),

  addLabel: (id: string, labelId: string) =>
    apiClient.put(`/userstories/${id}/labels/${labelId}`),

  removeLabel: (id: string, labelId: string) =>
    apiClient.delete(`/userstories/${id}/labels/${labelId}`),

  setRecurrence: (id: string, frequency: 'Daily' | 'Weekly' | 'Monthly' | null, endDate: string | null) =>
    apiClient.put(`/userstories/${id}/recurrence`, { frequency, endDate }),

  uploadAttachment: (id: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<Attachment>(`/userstories/${id}/attachments`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then((res) => res.data)
  },

  // Returns a blob URL — the download needs the Authorization header, so a
  // plain <a href> to the API wouldn't authenticate; axios attaches it via
  // the request interceptor like any other call, then we hand back an
  // object URL the browser can open/download from.
  downloadAttachment: (id: string, attachmentId: string) =>
    apiClient.get(`/userstories/${id}/attachments/${attachmentId}/download`, { responseType: 'blob' })
      .then((res) => URL.createObjectURL(res.data as Blob)),

  removeAttachment: (id: string, attachmentId: string) =>
    apiClient.delete(`/userstories/${id}/attachments/${attachmentId}`),

  getActivity: (id: string, limit = 50) =>
    apiClient.get<Activity[]>(`/userstories/${id}/activity`, { params: { limit } }).then((res) => res.data),

  getLinks: (id: string) =>
    apiClient.get<ResolvedStoryLink[]>(`/userstories/${id}/links`).then((res) => res.data),

  addLink: (id: string, linkedStoryId: string, linkType: 'Blocks' | 'RelatesTo') =>
    apiClient.post(`/userstories/${id}/links`, { linkedStoryId, linkType }),

  removeLink: (id: string, linkedStoryId: string) =>
    apiClient.delete(`/userstories/${id}/links/${linkedStoryId}`),

  exportCsv: (teamId: string, filters: UserStoryFilters, sprintId?: string, labelId?: string, showArchived = false) =>
    apiClient.get('/userstories/export', {
      params: { teamId, ...filters, sprintId, labelId, showArchived },
      responseType: 'blob',
    }).then((res) => {
      const url = URL.createObjectURL(res.data as Blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `stories-export-${new Date().toISOString().slice(0, 10)}.csv`
      link.click()
      URL.revokeObjectURL(url)
    }),

  setEstimate: (id: string, hours: number | null) =>
    apiClient.put(`/userstories/${id}/estimate`, { hours }),

  logTime: (id: string, hours: number, note: string | undefined) =>
    apiClient.post<TimeLogEntry>(`/userstories/${id}/time-logs`, { hours, note }).then((res) => res.data),

  analyzeCsv: (teamId: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<CsvAnalysis>('/userstories/import/analyze', formData, {
      params: { teamId },
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then((res) => res.data)
  },

  previewImport: (teamId: string, file: File, mapping: CsvColumnMapping) => {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('mapping', JSON.stringify(mapping))
    return apiClient.post<ImportRow[]>('/userstories/import/preview', formData, {
      params: { teamId },
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then((res) => res.data)
  },

  confirmImport: (teamId: string, file: File, mapping: CsvColumnMapping) => {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('mapping', JSON.stringify(mapping))
    return apiClient.post<ImportSummary>('/userstories/import/confirm', formData, {
      params: { teamId },
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then((res) => res.data)
  },
}

export interface CsvAnalysis {
  headers: string[]
  sampleRows: string[][]
  totalDataRows: number
}

export interface CsvColumnMapping {
  titleColumn: string
  descriptionColumn?: string
  statusColumn?: string
  priorityColumn?: string
  dueDateColumn?: string
  storyPointsColumn?: string
  labelsColumn?: string
  statusValueMap?: Record<string, string>
  priorityValueMap?: Record<string, string>
}

export interface ImportRow {
  rowNumber: number
  isValid: boolean
  error?: string
  title?: string
  description?: string
  status: string
  priority: string
  assigneeEmail?: string
  dueDate?: string
  storyPoints?: number
  labelNames: string[]
}

export interface ImportSummary {
  createdCount: number
  skippedCount: number
  rows: ImportRow[]
}

export interface ResolvedStoryLink {
  linkedStoryId: string
  linkedStoryTitle: string
  linkedStoryTeamId: string
  linkType: 'Blocks' | 'BlockedBy' | 'RelatesTo'
  linkedStoryIsDone: boolean
}
