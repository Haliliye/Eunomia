namespace TodoApp.Application.PersonalTasks.Queries.GetMyWork;

/// <summary>US-142: one unified shape for both kinds of "your work" — SourceType
/// distinguishes them, TeamId/TeamName are null for personal tasks.</summary>
public record MyWorkItemDto(
    string Id,
    string Title,
    string SourceType,
    bool IsCompleted,
    DateTime? DueDate,
    string? TeamId,
    string? TeamName);
