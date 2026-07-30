using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.DTOs;
using TodoApp.Domain.UserStories;

namespace TodoApp.Application.UserStories.Queries.GetUserStoriesByTeam;

public class GetUserStoriesByTeamQueryHandler : IRequestHandler<GetUserStoriesByTeamQuery, PagedResult<UserStoryDto>>
{
    private readonly IUserStoryRepository _userStoryRepository;

    public GetUserStoriesByTeamQueryHandler(IUserStoryRepository userStoryRepository)
    {
        _userStoryRepository = userStoryRepository;
    }

    public async Task<PagedResult<UserStoryDto>> Handle(GetUserStoriesByTeamQuery request, CancellationToken cancellationToken)
    {
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
