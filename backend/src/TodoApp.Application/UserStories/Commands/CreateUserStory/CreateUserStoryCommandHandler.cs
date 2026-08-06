using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.DTOs;
using TodoApp.Domain.Activities;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.CreateUserStory;

public class CreateUserStoryCommandHandler : IRequestHandler<CreateUserStoryCommand, UserStoryDto>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IActivityRepository _activityRepository;

    public CreateUserStoryCommandHandler(
        IUserStoryRepository userStoryRepository,
        ITeamRepository teamRepository,
        IRealtimeNotifier realtimeNotifier,
        IActivityRepository activityRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _realtimeNotifier = realtimeNotifier;
        _activityRepository = activityRepository;
    }

    public async Task<UserStoryDto> Handle(CreateUserStoryCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.CreatedByUserId);

        var story = UserStory.Create(
            id: Guid.NewGuid().ToString(),
            teamId: request.TeamId,
            title: request.Title,
            description: request.Description,
            createdByUserId: request.CreatedByUserId);

        await _userStoryRepository.AddAsync(story, cancellationToken);

        await _activityRepository.AddAsync(
            Activity.Create(Guid.NewGuid().ToString(), story.TeamId, request.CreatedByUserId, ActivityType.Created, $"created \"{story.Title}\"", story.Id),
            cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(story.TeamId, new { type = "storyChanged", storyId = story.Id }, cancellationToken);

        return UserStoryMapper.ToDto(story);
    }
}
