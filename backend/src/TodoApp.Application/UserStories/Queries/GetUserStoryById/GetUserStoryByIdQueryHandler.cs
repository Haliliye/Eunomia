using MediatR;
using TodoApp.Application.UserStories.DTOs;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Queries.GetUserStoryById;

public class GetUserStoryByIdQueryHandler : IRequestHandler<GetUserStoryByIdQuery, UserStoryDto?>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public GetUserStoryByIdQueryHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task<UserStoryDto?> Handle(GetUserStoryByIdQuery request, CancellationToken cancellationToken)
    {
        var story = await _userStoryRepository.GetByIdAsync(request.UserStoryId, cancellationToken);
        if (story is null) return null;

        // Previously missing entirely — any authenticated user could view any
        // story's full details just by knowing/guessing its id.
        var team = await _teamRepository.GetByIdAsync(story.TeamId, cancellationToken);
        team?.EnsureIsMember(request.RequestingUserId);

        return UserStoryMapper.ToDto(story);
    }
}
