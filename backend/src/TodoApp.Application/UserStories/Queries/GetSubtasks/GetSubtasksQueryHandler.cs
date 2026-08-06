using MediatR;
using TodoApp.Application.UserStories.DTOs;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Queries.GetSubtasks;

public class GetSubtasksQueryHandler : IRequestHandler<GetSubtasksQuery, IReadOnlyList<UserStoryDto>>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public GetSubtasksQueryHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<UserStoryDto>> Handle(GetSubtasksQuery request, CancellationToken cancellationToken)
    {
        var parent = await _userStoryRepository.GetByIdAsync(request.ParentStoryId, cancellationToken)
            ?? throw new KeyNotFoundException("Story not found.");

        var team = await _teamRepository.GetByIdAsync(parent.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var subtasks = await _userStoryRepository.GetByParentIdAsync(request.ParentStoryId, cancellationToken);
        return subtasks.OrderBy(s => s.CreatedOn).Select(UserStoryMapper.ToDto).ToList();
    }
}
