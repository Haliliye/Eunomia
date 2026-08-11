namespace TodoApp.Application.UserStories.Commands.ImportUserStories;

/// <summary>One parsed+validated CSV row — used for both the preview (US-147's
/// "preview... before confirming") and the actual import.</summary>
public record ImportRowDto(
    int RowNumber,
    bool IsValid,
    string? Error,
    string? Title,
    string? Description,
    string Status,
    string Priority,
    string? AssigneeEmail,
    DateTime? DueDate,
    int? StoryPoints,
    IReadOnlyList<string> LabelNames,
    string? JiraIssueKey = null,
    string? AzureDevOpsWorkItemId = null);

public record ImportSummaryDto(int CreatedCount, int SkippedCount, IReadOnlyList<ImportRowDto> Rows, int UpdatedCount = 0);
