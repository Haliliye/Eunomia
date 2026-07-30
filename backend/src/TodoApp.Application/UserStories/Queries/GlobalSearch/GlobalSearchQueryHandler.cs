using MediatR;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Queries.GlobalSearch;

public class GlobalSearchQueryHandler : IRequestHandler<GlobalSearchQuery, IReadOnlyList<GlobalSearchResultDto>>
{
    // Per-team and overall caps — a command-palette search should feel
    // instant and show only the most relevant handful, not everything.
    private const int PerTeamLimit = 5;
    private const int TotalLimit = 20;

    private readonly ITeamRepository _teamRepository;
    private readonly IUserStoryRepository _userStoryRepository;

    public GlobalSearchQueryHandler(ITeamRepository teamRepository, IUserStoryRepository userStoryRepository)
    {
        _teamRepository = teamRepository;
        _userStoryRepository = userStoryRepository;
    }

    public async Task<IReadOnlyList<GlobalSearchResultDto>> Handle(GlobalSearchQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword)) return Array.Empty<GlobalSearchResultDto>();

        var teams = await _teamRepository.GetByMemberIdAsync(request.RequestingUserId, cancellationToken);
        var results = new List<GlobalSearchResultDto>();

        foreach (var team in teams)
        {
            if (results.Count >= TotalLimit) break;

            var (stories, _) = await _userStoryRepository.SearchAsync(
                team.Id, status: null, priority: null, assigneeId: null, keyword: request.Keyword,
                page: 1, pageSize: PerTeamLimit, showArchived: false, cancellationToken: cancellationToken);

            results.AddRange(stories.Select(s => new GlobalSearchResultDto(s.Id, s.Title, team.Id, team.Name, s.Status.ToString())));
        }

        return results.Take(TotalLimit).ToList();
    }
}
