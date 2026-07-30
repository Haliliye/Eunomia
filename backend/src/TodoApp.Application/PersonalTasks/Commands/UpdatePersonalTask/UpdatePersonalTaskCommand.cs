using MediatR;

namespace TodoApp.Application.PersonalTasks.Commands.UpdatePersonalTask;

public record UpdatePersonalTaskCommand(string TaskId, string RequestingUserId, string Title, string? Description, DateTime? DueDate) : IRequest;
