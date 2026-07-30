using MediatR;
using TodoApp.Application.Activities.DTOs;
using TodoApp.Application.Common;
using TodoApp.Domain.Activities;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Activities.Queries.GetTeamActivity;

public class GetTeamActivityQueryHandler : IRequestHandler<GetTeamActivityQuery, PagedResult<ActivityDto>>
{
    private readonly IActivityRepository _activityRepository;
    private readonly ITeamRepository _teamRepository;

    public GetTeamActivityQueryHandler(IActivityRepository activityRepository, ITeamRepository teamRepository)
    {
        _activityRepository = activityRepository;
        _teamRepository = teamRepository;
    }

    public async Task<PagedResult<ActivityDto>> Handle(GetTeamActivityQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        ActivityType? type = null;
        if (!string.IsNullOrWhiteSpace(request.ActionType) && Enum.TryParse<ActivityType>(request.ActionType, out var parsed))
            type = parsed;

        var (items, totalCount) = await _activityRepository.SearchByTeamIdAsync(
            request.TeamId, request.ActorUserId, type, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(a => new ActivityDto(a.Id, a.ActorUserId, a.Type.ToString(), a.Message, a.RelatedEntityId, a.CreatedOn)).ToList();
        return new PagedResult<ActivityDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
