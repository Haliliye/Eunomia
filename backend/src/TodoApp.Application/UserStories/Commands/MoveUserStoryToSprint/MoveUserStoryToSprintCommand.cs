using MediatR;

namespace TodoApp.Application.UserStories.Commands.MoveUserStoryToSprint;

/// <summary>SprintId null moves the story back to the backlog.</summary>
public record MoveUserStoryToSprintCommand(string UserStoryId, string? SprintId, string RequestingUserId = "") : IRequest;
