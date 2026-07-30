using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.RemoveLabelFromUserStory;

public class RemoveLabelFromUserStoryCommandHandler : IRequestHandler<RemoveLabelFromUserStoryCommand>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public RemoveLabelFromUserStoryCommandHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository, IRealtimeNotifier realtimeNotifier)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task Handle(RemoveLabelFromUserStoryCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        story.RemoveLabel(request.LabelId);
        await _userStoryRepository.UpdateAsync(story, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(story.TeamId, new { type = "storyChanged", storyId = story.Id }, cancellationToken);
    }
}
