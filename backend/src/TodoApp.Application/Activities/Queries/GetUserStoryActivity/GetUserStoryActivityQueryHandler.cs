using MediatR;
using TodoApp.Application.Activities.DTOs;
using TodoApp.Domain.Activities;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.Activities.Queries.GetUserStoryActivity;

public class GetUserStoryActivityQueryHandler : IRequestHandler<GetUserStoryActivityQuery, IReadOnlyList<ActivityDto>>
{
    private readonly IActivityRepository _activityRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public GetUserStoryActivityQueryHandler(IActivityRepository activityRepository, IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _activityRepository = activityRepository;
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<ActivityDto>> Handle(GetUserStoryActivityQuery request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var activities = await _activityRepository.GetByRelatedEntityIdAsync(request.UserStoryId, request.Limit, cancellationToken);
        return activities.Select(a => new ActivityDto(a.Id, a.ActorUserId, a.Type.ToString(), a.Message, a.RelatedEntityId, a.CreatedOn)).ToList();
    }
}
