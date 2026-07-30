using MediatR;

namespace TodoApp.Application.UserStories.Commands.ChangeStatus;

public record ChangeUserStoryStatusCommand(string UserStoryId, string NewStatus, string ChangedByUserId = "") : IRequest;
