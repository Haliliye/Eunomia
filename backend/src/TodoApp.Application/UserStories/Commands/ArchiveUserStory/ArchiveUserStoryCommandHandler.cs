using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Activities;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.ArchiveUserStory;

public class ArchiveUserStoryCommandHandler : IRequestHandler<ArchiveUserStoryCommand>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IActivityRepository _activityRepository;

    public ArchiveUserStoryCommandHandler(
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

    public async Task Handle(ArchiveUserStoryCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.ArchivedByUserId);

        story.Archive();
        await _userStoryRepository.UpdateAsync(story, cancellationToken);

        if (!string.IsNullOrEmpty(request.ArchivedByUserId))
        {
            await _activityRepository.AddAsync(
                Activity.Create(Guid.NewGuid().ToString(), story.TeamId, request.ArchivedByUserId, ActivityType.Archived, $"archived \"{story.Title}\"", story.Id),
                cancellationToken);
        }

        await _realtimeNotifier.NotifyTeamAsync(story.TeamId, new { type = "storyChanged", storyId = story.Id }, cancellationToken);
    }
}
