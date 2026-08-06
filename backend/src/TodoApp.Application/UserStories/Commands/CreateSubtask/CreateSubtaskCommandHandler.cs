using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.DTOs;
using TodoApp.Domain.Activities;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.CreateSubtask;

public class CreateSubtaskCommandHandler : IRequestHandler<CreateSubtaskCommand, UserStoryDto>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IActivityRepository _activityRepository;

    public CreateSubtaskCommandHandler(
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

    public async Task<UserStoryDto> Handle(CreateSubtaskCommand request, CancellationToken cancellationToken)
    {
        var parent = await _userStoryRepository.GetByIdAsync(request.ParentStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("Parent story not found.");

        var team = await _teamRepository.GetByIdAsync(parent.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        // Matches Jira's own model — a subtask is a lightweight child item,
        // not a hierarchy node itself.
        if (parent.ParentId is not null)
            throw new InvalidOperationException("A subtask can't have its own subtasks.");

        var subtask = UserStory.Create(
            Guid.NewGuid().ToString(), parent.TeamId, request.Title, description: null,
            createdByUserId: request.RequestingUserId, parentId: parent.Id);

        await _userStoryRepository.AddAsync(subtask, cancellationToken);

        await _activityRepository.AddAsync(
            Activity.Create(Guid.NewGuid().ToString(), parent.TeamId, request.RequestingUserId, ActivityType.Created,
                $"added subtask \"{subtask.Title}\" to \"{parent.Title}\"", subtask.Id),
            cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(parent.TeamId, new { type = "storyChanged", storyId = subtask.Id }, cancellationToken);

        return UserStoryMapper.ToDto(subtask);
    }
}
