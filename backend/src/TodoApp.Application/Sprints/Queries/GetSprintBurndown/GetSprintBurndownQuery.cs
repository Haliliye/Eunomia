using MediatR;

namespace TodoApp.Application.Sprints.Queries.GetSprintBurndown;

public record GetSprintBurndownQuery(string SprintId, string RequestingUserId) : IRequest<SprintBurndownDto>;
