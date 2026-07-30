using MediatR;

namespace TodoApp.Application.UserStories.Commands.ChangePriority;

public record ChangeUserStoryPriorityCommand(string UserStoryId, string NewPriority, string RequestingUserId = "") : IRequest;
