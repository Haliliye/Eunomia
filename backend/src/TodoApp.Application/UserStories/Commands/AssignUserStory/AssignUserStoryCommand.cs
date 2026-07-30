using MediatR;

namespace TodoApp.Application.UserStories.Commands.AssignUserStory;

/// <summary>AssigneeId null means "unassign" (US-109 AC: "can be set back to Unassigned").</summary>
public record AssignUserStoryCommand(string UserStoryId, string? AssigneeId, string AssignedByUserId = "") : IRequest;
