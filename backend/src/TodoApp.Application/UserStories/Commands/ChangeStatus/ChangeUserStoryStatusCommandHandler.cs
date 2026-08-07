using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Activities;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.ChangeStatus;

public class ChangeUserStoryStatusCommandHandler : IRequestHandler<ChangeUserStoryStatusCommand>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IActivityRepository _activityRepository;
    private readonly IMediator _mediator;

    public ChangeUserStoryStatusCommandHandler(
        IUserStoryRepository userStoryRepository,
        ITeamRepository teamRepository,
        IRealtimeNotifier realtimeNotifier,
        IActivityRepository activityRepository,
        IMediator mediator)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _realtimeNotifier = realtimeNotifier;
        _activityRepository = activityRepository;
        _mediator = mediator;
    }

    public async Task Handle(ChangeUserStoryStatusCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        // Baseline authorization — the actor id was previously only used for
        // activity-log attribution, never actually checked against team membership.
        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.ChangedByUserId);

        if (!team.Columns.Any(c => c.Key == request.NewStatus))
            throw new ArgumentException($"Unknown status '{request.NewStatus}'.");

        var previousStatus = story.Status;

        // Team.ChangeStatus enforces the allowed workflow transitions
        // (To Do <-> In Progress <-> Done) and throws InvalidOperationException
        // for an illegal transition — let that surface as a 400 in the controller.
        story.ChangeStatus(request.NewStatus);

        await _userStoryRepository.UpdateAsync(story, cancellationToken);

        if (!string.IsNullOrEmpty(request.ChangedByUserId))
        {
            await _activityRepository.AddAsync(
                Activity.Create(Guid.NewGuid().ToString(), story.TeamId, request.ChangedByUserId, ActivityType.StatusChanged,
                    $"moved \"{story.Title}\" from {previousStatus} to {story.Status}", story.Id),
                cancellationToken);
        }

        // US-129: a recurring story that just became Done spawns its next
        // occurrence automatically — CreateNextOccurrence returns null if this
        // story isn't recurring, or its recurrence end date has passed.
        if (request.NewStatus == "Done")
        {
            var nextOccurrence = story.CreateNextOccurrence(Guid.NewGuid().ToString());
            if (nextOccurrence is not null)
            {
                await _userStoryRepository.AddAsync(nextOccurrence, cancellationToken);
                // Dispatches UserStoryAssignedEvent if the occurrence carried over an assignee.
                await _mediator.PublishDomainEventsAsync(nextOccurrence, cancellationToken);

                await _activityRepository.AddAsync(
                    Activity.Create(Guid.NewGuid().ToString(), story.TeamId, request.ChangedByUserId, ActivityType.StatusChanged,
                        $"completed recurring story \"{story.Title}\" — next occurrence created", nextOccurrence.Id),
                    cancellationToken);
            }
        }

        // Live board update: anyone else with this team's board open should
        // see the card move without refreshing (the payload is deliberately
        // minimal — the frontend just refetches on receiving it).
        await _realtimeNotifier.NotifyTeamAsync(story.TeamId, new { type = "storyChanged", storyId = story.Id }, cancellationToken);
    }
}
