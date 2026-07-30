using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.UpdateUserStory;

public class UpdateUserStoryCommandHandler : IRequestHandler<UpdateUserStoryCommand>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public UpdateUserStoryCommandHandler(
        IUserStoryRepository userStoryRepository,
        ITeamRepository teamRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task Handle(UpdateUserStoryCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        // Baseline authorization: without this, any authenticated user could
        // edit any team's stories just by knowing/guessing a story id.
        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        // US-107 AC: concurrent edits shouldn't silently overwrite each other.
        // The client sends back the Version it loaded; if it no longer matches
        // what's persisted, someone else saved a change in between.
        if (story.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "This story was changed by someone else since you loaded it. Reload and try again.");
        }

        story.UpdateDetails(request.Title, request.Description);
        story.SetDueDate(request.DueDate);
        story.SetStoryPoints(request.StoryPoints);

        var updated = await _userStoryRepository.UpdateWithConcurrencyCheckAsync(story, request.ExpectedVersion, cancellationToken);
        if (!updated)
        {
            // Someone else's write landed between our read and this write —
            // the in-memory check above passed but the database-level check didn't.
            throw new ConcurrencyConflictException(
                "This story was changed by someone else since you loaded it. Reload and try again.");
        }

        await _realtimeNotifier.NotifyTeamAsync(story.TeamId, new { type = "storyChanged", storyId = story.Id }, cancellationToken);
    }
}
