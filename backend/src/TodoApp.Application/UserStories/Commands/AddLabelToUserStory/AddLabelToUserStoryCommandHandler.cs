using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.AddLabelToUserStory;

public class AddLabelToUserStoryCommandHandler : IRequestHandler<AddLabelToUserStoryCommand>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public AddLabelToUserStoryCommandHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository, IRealtimeNotifier realtimeNotifier)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task Handle(AddLabelToUserStoryCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        if (team.Labels.All(l => l.Id != request.LabelId))
            throw new KeyNotFoundException("Label not found on this team.");

        story.AddLabel(request.LabelId);
        await _userStoryRepository.UpdateAsync(story, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(story.TeamId, new { type = "storyChanged", storyId = story.Id }, cancellationToken);
    }
}
