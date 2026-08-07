using MediatR;
using TodoApp.Domain.PersonalTasks;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.PersonalTasks.Queries.GetMyWork;

public class GetMyWorkQueryHandler : IRequestHandler<GetMyWorkQuery, IReadOnlyList<MyWorkItemDto>>
{
    private readonly IPersonalTaskRepository _personalTaskRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public GetMyWorkQueryHandler(IPersonalTaskRepository personalTaskRepository, IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _personalTaskRepository = personalTaskRepository;
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<MyWorkItemDto>> Handle(GetMyWorkQuery request, CancellationToken cancellationToken)
    {
        var items = new List<MyWorkItemDto>();

        var personalTasks = await _personalTaskRepository.GetByOwnerIdAsync(request.UserId, cancellationToken);
        foreach (var task in personalTasks.Where(t => t.ConvertedToUserStoryId is null))
        {
            items.Add(new MyWorkItemDto(task.Id, task.Title, "Personal", task.IsCompleted, task.DueDate, null, null));
        }

        var assignedStories = await _userStoryRepository.GetByAssigneeIdAsync(request.UserId, cancellationToken);
        var teamNameCache = new Dictionary<string, string>();

        foreach (var story in assignedStories)
        {
            if (!teamNameCache.TryGetValue(story.TeamId, out var teamName))
            {
                var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken);
                teamName = team?.Name ?? "Unknown team";
                teamNameCache[story.TeamId] = teamName;
            }

            items.Add(new MyWorkItemDto(story.Id, story.Title, "TeamStory", story.Status == "Done", story.DueDate, story.TeamId, teamName));
        }

        // Incomplete items first, then by due date (soonest first, undated last).
        return items
            .OrderBy(i => i.IsCompleted)
            .ThenBy(i => i.DueDate ?? DateTime.MaxValue)
            .ToList();
    }
}
