using MediatR;
using TodoApp.Application.Activities.DTOs;
using TodoApp.Application.Common;

namespace TodoApp.Application.Activities.Queries.GetTeamActivity;

/// <summary>US-132: team-wide feed, paginated. US-133: ActorUserId and/or
/// ActionType optionally filter it (combinable).</summary>
public record GetTeamActivityQuery(
    string TeamId,
    string RequestingUserId,
    int Page = 1,
    int PageSize = 20,
    string? ActorUserId = null,
    string? ActionType = null) : IRequest<PagedResult<ActivityDto>>;
