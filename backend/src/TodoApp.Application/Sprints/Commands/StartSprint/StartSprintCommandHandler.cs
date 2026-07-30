using MediatR;
using TodoApp.Domain.Sprints;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.Sprints.Commands.StartSprint;

public class StartSprintCommandHandler : IRequestHandler<StartSprintCommand>
{
    private readonly ISprintRepository _sprintRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;

    public StartSprintCommandHandler(ISprintRepository sprintRepository, ITeamRepository teamRepository, IUserStoryRepository userStoryRepository)
    {
        _sprintRepository = sprintRepository;
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
    }

    public async Task Handle(StartSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdAsync(request.SprintId, cancellationToken)
            ?? throw new KeyNotFoundException("Sprint not found.");

        var team = await _teamRepository.GetByIdAsync(sprint.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        // Only one sprint can be Active per team at a time — keeps "what's the
        // team working on right now" unambiguous. Complete (or hadn't started)
        // the current one before starting another.
        var existingActive = await _sprintRepository.GetActiveByTeamIdAsync(sprint.TeamId, cancellationToken);
        if (existingActive is not null && existingActive.Id != sprint.Id)
            throw new InvalidOperationException($"\"{existingActive.Name}\" is already active — complete it before starting another sprint.");

        // The burndown chart's "ideal" line is drawn from this starting total
        // down to 0 — whatever's already planned into the sprint at the
        // moment it starts, in story points (unestimated stories count as 0).
        var (plannedStories, _) = await _userStoryRepository.SearchAsync(
            sprint.TeamId, status: null, priority: null, assigneeId: null, keyword: null,
            page: 1, pageSize: 500, showArchived: false, sprintId: sprint.Id, cancellationToken: cancellationToken);
        var totalPointsAtStart = plannedStories.Sum(s => s.StoryPoints ?? 0);

        sprint.Start(totalPointsAtStart);
        await _sprintRepository.UpdateAsync(sprint, cancellationToken);
    }
}
