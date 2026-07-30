using MediatR;

namespace TodoApp.Application.Sprints.Commands.StartSprint;

public record StartSprintCommand(string SprintId, string RequestingUserId) : IRequest;
