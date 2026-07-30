using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Activities;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.AssignUserStory;

public class AssignUserStoryCommandHandler : IRequestHandler<AssignUserStoryCommand>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IMediator _mediator;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IActivityRepository _activityRepository;

    public AssignUserStoryCommandHandler(
        IUserStoryRepository userStoryRepository,
        ITeamRepository teamRepository,
        IMediator mediator,
        IRealtimeNotifier realtimeNotifier,
        IActivityRepository activityRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _mediator = mediator;
        _realtimeNotifier = realtimeNotifier;
        _activityRepository = activityRepository;
    }

    public async Task Handle(AssignUserStoryCommand request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        // Fetched unconditionally now (not just when assigning someone) since
        // we need it to check the requester's own membership too.
        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.AssignedByUserId);

        if (request.AssigneeId is not null)
        {
            // US-109 AC: only members of the same team can be selected as assignees.
            var isMember = team.Members.Any(m => m.UserId == request.AssigneeId);
            if (!isMember)
                throw new InvalidOperationException("Only members of this story's team can be assigned to it.");
        }

        // Assign() raises UserStoryAssignedEvent internally when assigneeId isn't
        // null — the personal notification (US-118) is created by
        // UserStoryAssignedEventHandler once that event is dispatched below.
        story.Assign(request.AssigneeId);

        await _userStoryRepository.UpdateAsync(story, cancellationToken);
        await _mediator.PublishDomainEventsAsync(story, cancellationToken);

        if (!string.IsNullOrEmpty(request.AssignedByUserId))
        {
            var message = request.AssigneeId is not null
                ? $"assigned \"{story.Title}\" to {request.AssigneeId}"
                : $"unassigned \"{story.Title}\"";

            await _activityRepository.AddAsync(
                Activity.Create(Guid.NewGuid().ToString(), story.TeamId, request.AssignedByUserId, ActivityType.Assigned, message, story.Id),
                cancellationToken);
        }

        // Separately, broadcast to the team so anyone with the board open sees
        // the assignee change live (this is a board-refresh signal, not a
        // personal notification, so it goes through IRealtimeNotifier directly
        // rather than another domain event).
        await _realtimeNotifier.NotifyTeamAsync(story.TeamId, new { type = "storyChanged", storyId = story.Id }, cancellationToken);
    }
}
