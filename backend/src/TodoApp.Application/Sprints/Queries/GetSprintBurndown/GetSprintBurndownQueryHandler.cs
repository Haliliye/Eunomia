using MediatR;
using TodoApp.Domain.Sprints;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.Sprints.Queries.GetSprintBurndown;

public class GetSprintBurndownQueryHandler : IRequestHandler<GetSprintBurndownQuery, SprintBurndownDto>
{
    private readonly ISprintRepository _sprintRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;

    public GetSprintBurndownQueryHandler(ISprintRepository sprintRepository, ITeamRepository teamRepository, IUserStoryRepository userStoryRepository)
    {
        _sprintRepository = sprintRepository;
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
    }

    public async Task<SprintBurndownDto> Handle(GetSprintBurndownQuery request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdAsync(request.SprintId, cancellationToken)
            ?? throw new KeyNotFoundException("Sprint not found.");

        var team = await _teamRepository.GetByIdAsync(sprint.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        // Only a started sprint has a meaningful burndown — a Planned one has
        // no TotalPointsAtStart yet and nothing to chart.
        if (sprint.Status != SprintStatus.Planned && sprint.TotalPointsAtStart.HasValue)
        {
            var (stories, _) = await _userStoryRepository.SearchAsync(
                sprint.TeamId, status: null, priority: null, assigneeId: null, keyword: null,
                page: 1, pageSize: 500, showArchived: false, sprintId: sprint.Id, cancellationToken: cancellationToken);

            var remaining = stories.Where(s => s.Status != UserStoryStatus.Done).ToList();
            var remainingCount = remaining.Count;
            var remainingPoints = remaining.Sum(s => s.StoryPoints ?? 0);

            // Recorded once per calendar day — a view today just updates
            // today's point with the latest numbers rather than adding a
            // duplicate, so the chart reflects "as of now" without drifting.
            sprint.RecordSnapshot(DateOnly.FromDateTime(DateTime.UtcNow), remainingCount, remainingPoints);
            await _sprintRepository.UpdateAsync(sprint, cancellationToken);
        }

        return new SprintBurndownDto(
            DateOnly.FromDateTime(sprint.StartDate),
            DateOnly.FromDateTime(sprint.EndDate),
            sprint.TotalPointsAtStart ?? 0,
            sprint.BurndownSnapshots.Select(s => new BurndownPointDto(s.Date, s.RemainingCount, s.RemainingPoints)).ToList());
    }
}
