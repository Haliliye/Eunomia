using MediatR;
using TodoApp.Application.Sprints.DTOs;

namespace TodoApp.Application.Sprints.Commands.CompleteSprint;

public record CompleteSprintCommand(string SprintId, string RequestingUserId) : IRequest<SprintCompletionSummaryDto>;
