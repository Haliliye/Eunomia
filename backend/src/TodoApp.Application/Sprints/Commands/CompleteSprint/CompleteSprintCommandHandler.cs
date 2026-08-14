using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Sprints.DTOs;
using TodoApp.Domain.Sprints;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.Sprints.Commands.CompleteSprint;

public class CompleteSprintCommandHandler : IRequestHandler<CompleteSprintCommand, SprintCompletionSummaryDto>
{
    private readonly ISprintRepository _sprintRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public CompleteSprintCommandHandler(
        ISprintRepository sprintRepository,
        IUserStoryRepository userStoryRepository,
        ITeamRepository teamRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _sprintRepository = sprintRepository;
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<SprintCompletionSummaryDto> Handle(CompleteSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdAsync(request.SprintId, cancellationToken)
            ?? throw new KeyNotFoundException("Sprint not found.");

        var team = await _teamRepository.GetByIdAsync(sprint.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var (sprintStories, _) = await _userStoryRepository.SearchAsync(
            sprint.TeamId, status: null, priority: null, assigneeId: null, keyword: null,
            page: 1, pageSize: 500, showArchived: false, sprintId: sprint.Id, cancellationToken: cancellationToken);

        // Captured before the rollover below moves anything — this is a
        // snapshot of what was actually Done AT the moment of completion,
        // which is what a team velocity chart plots across sprints, and
        // what the closure summary (below) reports back to the person
        // completing it.
        var completedStories = sprintStories.Where(s => s.Status == "Done").ToList();
        var carriedOverStories = sprintStories.Where(s => s.Status != "Done").ToList();
        var completedPoints = completedStories.Sum(s => s.StoryPoints ?? 0);

        sprint.Complete(completedPoints);
        await _sprintRepository.UpdateAsync(sprint, cancellationToken);

        // Standard Scrum practice: anything not Done when the sprint ends goes
        // back to the backlog (unsprinted) rather than staying attached to a
        // now-closed sprint — it's fair game to be re-planned into the next one.
        foreach (var story in carriedOverStories)
        {
            story.MoveToSprint(null);
            await _userStoryRepository.UpdateAsync(story, cancellationToken);
        }

        await _realtimeNotifier.NotifyTeamAsync(sprint.TeamId, new { type = "sprintChanged", sprintId = sprint.Id }, cancellationToken);

        return new SprintCompletionSummaryDto(
            sprint.Id,
            sprint.Name,
            completedStories.Count,
            completedPoints,
            carriedOverStories.Count,
            carriedOverStories.Sum(s => s.StoryPoints ?? 0),
            carriedOverStories.Select(s => new CarriedOverStoryDto(s.Id, s.Title, s.Status)).ToList());
    }
}
