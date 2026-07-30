using MediatR;

namespace TodoApp.Application.UserStories.Commands.UpdateUserStory;

/// <summary>
/// ExpectedVersion is the Version the client last loaded (from UserStoryDto) —
/// used for optimistic concurrency (US-107). See UpdateUserStoryCommandHandler.
/// </summary>
public record UpdateUserStoryCommand(
    string UserStoryId,
    string Title,
    string? Description,
    DateTime? DueDate,
    int? StoryPoints,
    int ExpectedVersion,
    string RequestingUserId) : IRequest;
