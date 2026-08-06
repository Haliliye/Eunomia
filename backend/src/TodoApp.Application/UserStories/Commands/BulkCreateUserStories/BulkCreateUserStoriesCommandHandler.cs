using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.DTOs;
using TodoApp.Domain.Activities;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Commands.BulkCreateUserStories;

public class BulkCreateUserStoriesCommandHandler : IRequestHandler<BulkCreateUserStoriesCommand, IReadOnlyList<UserStoryDto>>
{
    // A sane upper bound on a single paste — protects against someone
    // accidentally pasting a huge file and creating thousands of stories.
    private const int MaxTitlesPerRequest = 200;

    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public BulkCreateUserStoriesCommandHandler(
        IUserStoryRepository userStoryRepository,
        ITeamRepository teamRepository,
        IActivityRepository activityRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
        _activityRepository = activityRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<IReadOnlyList<UserStoryDto>> Handle(BulkCreateUserStoriesCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        // Blank lines (common when pasting from a spreadsheet or a
        // trailing newline) are silently skipped rather than rejected —
        // the AC here is "each non-empty line becomes a story", not "every
        // line must be non-empty".
        var titles = request.Titles
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Take(MaxTitlesPerRequest)
            .ToList();

        var created = new List<UserStoryDto>();
        foreach (var title in titles)
        {
            var story = UserStory.Create(Guid.NewGuid().ToString(), team.Id, title, null, createdByUserId: request.RequestingUserId);
            await _userStoryRepository.AddAsync(story, cancellationToken);

            await _activityRepository.AddAsync(
                Activity.Create(Guid.NewGuid().ToString(), team.Id, request.RequestingUserId, ActivityType.Created, $"created \"{story.Title}\"", story.Id),
                cancellationToken);

            created.Add(UserStoryMapper.ToDto(story));
        }

        if (created.Count > 0)
            await _realtimeNotifier.NotifyTeamAsync(team.Id, new { type = "storyChanged", storyId = (string?)null }, cancellationToken);

        return created;
    }
}
