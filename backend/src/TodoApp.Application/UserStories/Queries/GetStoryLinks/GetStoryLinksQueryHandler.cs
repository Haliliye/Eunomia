using MediatR;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Queries.GetStoryLinks;

public class GetStoryLinksQueryHandler : IRequestHandler<GetStoryLinksQuery, IReadOnlyList<ResolvedStoryLinkDto>>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public GetStoryLinksQueryHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<ResolvedStoryLinkDto>> Handle(GetStoryLinksQuery request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.StoryId, cancellationToken)
            ?? throw new KeyNotFoundException("User story not found.");

        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var results = new List<ResolvedStoryLinkDto>();
        foreach (var link in story.Links)
        {
            var linkedStory = await _userStoryRepository.GetByIdAsync(link.LinkedStoryId, cancellationToken);
            if (linkedStory is null) continue; // the linked story may have since been deleted — just omit it

            results.Add(new ResolvedStoryLinkDto(
                linkedStory.Id, linkedStory.Title, linkedStory.TeamId, link.LinkType.ToString(),
                linkedStory.Status == UserStoryStatus.Done));
        }

        return results;
    }
}
