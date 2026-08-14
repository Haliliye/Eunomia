using MediatR;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Domain.Sprints;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.Teams.Queries.GetPortfolioOverview;

public class GetPortfolioOverviewQueryHandler : IRequestHandler<GetPortfolioOverviewQuery, IReadOnlyList<TeamPortfolioSummaryDto>>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ISprintRepository _sprintRepository;

    public GetPortfolioOverviewQueryHandler(ITeamRepository teamRepository, IUserStoryRepository userStoryRepository, ISprintRepository sprintRepository)
    {
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
        _sprintRepository = sprintRepository;
    }

    public async Task<IReadOnlyList<TeamPortfolioSummaryDto>> Handle(GetPortfolioOverviewQuery request, CancellationToken cancellationToken)
    {
        // No membership check needed beyond this — GetByMemberIdAsync already
        // scopes the result to teams the requester actually belongs to, same
        // reasoning as GlobalSearchQueryHandler.
        var teams = await _teamRepository.GetByMemberIdAsync(request.RequestingUserId, cancellationToken);

        var summaries = new List<TeamPortfolioSummaryDto>();
        var now = DateTime.UtcNow;

        foreach (var team in teams)
        {
            // GetByTeamIdAsync (unlike SearchAsync) doesn't filter out
            // archived stories or subtasks on its own — filtered here so the
            // portfolio's counts match what someone actually sees on that
            // team's backlog/board, not an inflated raw total.
            var stories = (await _userStoryRepository.GetByTeamIdAsync(team.Id, cancellationToken))
                .Where(s => !s.IsArchived && s.ParentId is null)
                .ToList();
            var doneCount = stories.Count(s => s.Status == "Done");
            var overdueCount = stories.Count(s => s.Status != "Done" && s.DueDate is not null && s.DueDate < now);

            var activeSprint = await _sprintRepository.GetActiveByTeamIdAsync(team.Id, cancellationToken);

            summaries.Add(new TeamPortfolioSummaryDto(
                team.Id,
                team.Name,
                team.Members.Count,
                stories.Count,
                doneCount,
                overdueCount,
                activeSprint?.Name,
                activeSprint?.EndDate));
        }

        return summaries;
    }
}
