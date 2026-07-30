namespace TodoApp.Application.UserStories.DTOs;

public record TeamDashboardDto(
    IReadOnlyDictionary<string, int> CountsByStatus,
    IReadOnlyDictionary<string, int> CountsByAssignee,
    int TotalCount);
