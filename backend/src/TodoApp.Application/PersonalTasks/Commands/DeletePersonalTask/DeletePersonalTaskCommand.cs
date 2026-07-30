using MediatR;

namespace TodoApp.Application.PersonalTasks.Commands.DeletePersonalTask;

public record DeletePersonalTaskCommand(string TaskId, string RequestingUserId) : IRequest;
