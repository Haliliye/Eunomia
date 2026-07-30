using MediatR;

namespace TodoApp.Application.Sprints.Commands.CompleteSprint;

public record CompleteSprintCommand(string SprintId, string RequestingUserId) : IRequest;
