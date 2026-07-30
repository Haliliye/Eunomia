using MediatR;
using TodoApp.Application.Sprints.DTOs;

namespace TodoApp.Application.Sprints.Queries.GetTeamSprints;

public record GetTeamSprintsQuery(string TeamId, string RequestingUserId) : IRequest<IReadOnlyList<SprintDto>>;
