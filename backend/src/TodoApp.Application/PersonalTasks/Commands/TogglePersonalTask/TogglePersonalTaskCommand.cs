using MediatR;

namespace TodoApp.Application.PersonalTasks.Commands.TogglePersonalTask;

public record TogglePersonalTaskCommand(string TaskId, string RequestingUserId, bool IsCompleted) : IRequest;
