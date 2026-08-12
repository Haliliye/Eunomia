using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.DTOs;

namespace TodoApp.Application.UserStories.Queries.GetUserStoriesByTeam;

/// <summary>
/// Status/Priority/AssigneeId/Keyword are all optional filters (US-115, US-116) —
/// null/empty means "don't filter on this field". Multiple filters combine with AND.
/// Page/PageSize keep large backlogs from being fetched in one response.
/// ShowArchived flips the default (hidden) archived stories into view — used
/// by the Archived tab.
/// </summary>
public record GetUserStoriesByTeamQuery(
    string TeamId,
    string RequestingUserId,
    string? Status = null,
    string? Priority = null,
    string? AssigneeId = null,
    string? Keyword = null,
    int Page = 1,
    int PageSize = 25,
    bool ShowArchived = false,
    string? SprintId = null,
    string? LabelId = null) : IRequest<PagedResult<UserStoryDto>>;
