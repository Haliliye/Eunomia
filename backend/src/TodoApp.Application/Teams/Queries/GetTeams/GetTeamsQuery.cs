using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Teams.DTOs;

namespace TodoApp.Application.Teams.Queries.GetTeams;

public record GetTeamsQuery(string UserId, int Page = 1, int PageSize = 25) : IRequest<PagedResult<TeamDto>>;
