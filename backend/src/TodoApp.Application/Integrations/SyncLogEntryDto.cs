namespace TodoApp.Application.Integrations;

/// <summary>Shared between Jira and Azure DevOps sync status responses — same shape either way.</summary>
public record SyncLogEntryDto(DateTime SyncedOn, int CreatedCount, int UpdatedCount, int SkippedCount);
