namespace TodoApp.Application.Sprints.DTOs;

/// <summary>
/// What actually happened when a sprint was closed — shown to the person
/// completing it right after the action, since "X stories were completed,
/// Y carried over" isn't visible anywhere else once the sprint moves to
/// Completed and its stories scatter (back to the backlog, or staying Done).
/// </summary>
public record SprintCompletionSummaryDto(
    string SprintId,
    string SprintName,
    int CompletedCount,
    int CompletedPoints,
    int CarriedOverCount,
    int CarriedOverPoints,
    IReadOnlyList<CarriedOverStoryDto> CarriedOverStories);

public record CarriedOverStoryDto(string Id, string Title, string Status);
