using MediatR;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Queries.GetTeamTimeReport;

public class GetTeamTimeReportQueryHandler : IRequestHandler<GetTeamTimeReportQuery, TeamTimeReportDto>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public GetTeamTimeReportQueryHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task<TeamTimeReportDto> Handle(GetTeamTimeReportQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var stories = await _userStoryRepository.GetByTeamIdAsync(request.TeamId, cancellationToken);

        var rows = new List<StoryTimeReportRow>();
        foreach (var story in stories)
        {
            // Only entries within the selected date range count toward this
            // report's totals — the estimate itself isn't date-scoped (it's a
            // property of the story, not a point-in-time event).
            var loggedHours = story.TimeLogEntries
                .Where(t => (!request.StartDate.HasValue || t.LoggedOn >= request.StartDate.Value)
                         && (!request.EndDate.HasValue || t.LoggedOn <= request.EndDate.Value))
                .Sum(t => t.Hours);

            if (story.EstimatedHours is null && loggedHours == 0) continue; // nothing to report for this story

            var variance = story.EstimatedHours.HasValue ? loggedHours - story.EstimatedHours.Value : (double?)null;
            rows.Add(new StoryTimeReportRow(story.Id, story.Title, story.EstimatedHours, loggedHours, variance));
        }

        return new TeamTimeReportDto(rows, rows.Sum(r => r.EstimatedHours ?? 0), rows.Sum(r => r.LoggedHours));
    }
}
