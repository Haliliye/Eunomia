namespace TodoApp.Application.UserStories.DTOs;

public record ChecklistItemDto(string Id, string Text, bool IsCompleted, int Order);
public record TimeLogEntryDto(string Id, double Hours, string? Note, string LoggedByUserId, DateTime LoggedOn);
public record StoryLinkDto(string LinkedStoryId, string LinkType);

public record UserStoryDto(
    string Id,
    string TeamId,
    string Title,
    string? Description,
    string Status,
    string Priority,
    string? AssigneeId,
    DateTime? DueDate,
    DateTime CreatedOn,
    int Version,
    bool IsArchived,
    int? StoryPoints,
    string? SprintId,
    IReadOnlyList<ChecklistItemDto> ChecklistItems,
    IReadOnlyList<string> LabelIds,
    string? RecurrenceFrequency,
    DateTime? RecurrenceEndDate,
    IReadOnlyList<AttachmentDto> Attachments,
    double? EstimatedHours,
    IReadOnlyList<TimeLogEntryDto> TimeLogEntries,
    double TotalLoggedHours,
    IReadOnlyList<StoryLinkDto> Links,
    string? CreatedByUserId,
    string? ParentId);
