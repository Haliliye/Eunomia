using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.SetRecurrence;

public class SetRecurrenceCommandHandler : IRequestHandler<SetRecurrenceCommand>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public SetRecurrenceCommandHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository, IRealtimeNotifier realtimeNotifier)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task Handle(SetRecurrenceCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        RecurrenceFrequency? frequency = null;
        if (!string.IsNullOrWhiteSpace(request.Frequency))
        {
            if (!Enum.TryParse<RecurrenceFrequency>(request.Frequency, out var parsed))
                throw new ArgumentException($"Unknown recurrence frequency '{request.Frequency}'.");
            frequency = parsed;
        }

        // Changing frequency only affects future occurrences (US-130 AC) — this
        // is naturally true here since we only ever mutate THIS story's own
        // settings; any occurrence already spawned from it is a separate,
        // already-saved UserStory unaffected by this call.
        story.SetRecurrence(frequency, request.EndDate);
        await _userStoryRepository.UpdateAsync(story, cancellationToken);

        await _realtimeNotifier.NotifyTeamAsync(story.TeamId, new { type = "storyChanged", storyId = story.Id }, cancellationToken);
    }
}
