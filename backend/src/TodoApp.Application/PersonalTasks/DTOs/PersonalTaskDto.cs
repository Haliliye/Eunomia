namespace TodoApp.Application.PersonalTasks.DTOs;

public record PersonalTaskDto(
    string Id,
    string Title,
    string? Description,
    DateTime? DueDate,
    bool IsCompleted,
    DateTime CreatedOn,
    string? ConvertedToUserStoryId);
