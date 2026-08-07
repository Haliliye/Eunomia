using MediatR;
using TodoApp.Application.UserStories.DTOs;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Queries.GetTeamDashboard;

public class GetTeamDashboardQueryHandler : IRequestHandler<GetTeamDashboardQuery, TeamDashboardDto>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public GetTeamDashboardQueryHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task<TeamDashboardDto> Handle(GetTeamDashboardQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        // Previously missing entirely — same gap as GetTeamById/GetUserStoryById/
        // GetTeamActivity had before those were fixed.
        team.EnsureIsMember(request.RequestingUserId);

        var allStories = await _userStoryRepository.GetByTeamIdAsync(request.TeamId, cancellationToken);
        var stories = request.SprintId is null
            ? allStories
            : allStories.Where(s => s.SprintId == request.SprintId).ToList();

        // US-117 AC: counts per status, and a breakdown of open items per assignee.
        var countsByStatus = team.Columns
            .ToDictionary(c => c.Key, c => stories.Count(story => story.Status == c.Key));

        var openStories = stories.Where(s => s.Status != "Done");
        var countsByAssignee = openStories
            .GroupBy(s => s.AssigneeId ?? "Unassigned")
            .ToDictionary(g => g.Key, g => g.Count());

        return new TeamDashboardDto(countsByStatus, countsByAssignee, stories.Count);
    }
}
