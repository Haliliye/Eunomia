using MediatR;
using TodoApp.Application.Sprints.DTOs;

namespace TodoApp.Application.Sprints.Commands.CreateSprint;

public record CreateSprintCommand(string TeamId, string Name, DateTime StartDate, DateTime EndDate, string RequestingUserId) : IRequest<SprintDto>;
