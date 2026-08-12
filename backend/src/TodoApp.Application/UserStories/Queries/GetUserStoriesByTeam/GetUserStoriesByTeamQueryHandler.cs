using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.DTOs;
using TodoApp.Domain.Teams;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Queries.GetUserStoriesByTeam;

public class GetUserStoriesByTeamQueryHandler : IRequestHandler<GetUserStoriesByTeamQuery, PagedResult<UserStoryDto>>
{
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly ITeamRepository _teamRepository;

    public GetUserStoriesByTeamQueryHandler(IUserStoryRepository userStoryRepository, ITeamRepository teamRepository)
    {
        _userStoryRepository = userStoryRepository;
        _teamRepository = teamRepository;
    }

    public async Task<PagedResult<UserStoryDto>> Handle(GetUserStoriesByTeamQuery request, CancellationToken cancellationToken)
    {
        // Previously missing entirely — any authenticated user could list any
        // team's full backlog just by knowing/guessing its id (this is the
        // query the Backlog and Board pages both call). Only members get to.
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        // Clamp so a caller can't request an unbounded/huge page.
        var page = Math.Max(1, request.Page);
        // Ceiling of 500 (not just the paginated list's usual 25) so BoardPage
        // can request the whole backlog in one call without hitting an
        // artificially low cap — still bounded so no one can request unbounded rows.
        var pageSize = Math.Clamp(request.PageSize, 1, 500);

        var (items, totalCount) = await _userStoryRepository.SearchAsync(
            request.TeamId, request.Status, request.Priority, request.AssigneeId, request.Keyword,
            page, pageSize, request.ShowArchived, request.SprintId, request.LabelId, cancellationToken);

        return new PagedResult<UserStoryDto>(items.Select(UserStoryMapper.ToDto).ToList(), totalCount, page, pageSize);
    }
}
