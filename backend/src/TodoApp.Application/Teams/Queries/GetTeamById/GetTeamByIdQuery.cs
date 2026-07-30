using MediatR;
using TodoApp.Application.Teams.DTOs;

namespace TodoApp.Application.Teams.Queries.GetTeamById;

public record GetTeamByIdQuery(string TeamId, string RequestingUserId) : IRequest<TeamDto?>;
