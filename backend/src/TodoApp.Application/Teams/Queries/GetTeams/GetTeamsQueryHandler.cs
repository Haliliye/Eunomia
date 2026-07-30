using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Queries.GetTeams;

public class GetTeamsQueryHandler : IRequestHandler<GetTeamsQuery, PagedResult<TeamDto>>
{
    private readonly ITeamRepository _teamRepository;

    public GetTeamsQueryHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<PagedResult<TeamDto>> Handle(GetTeamsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (teams, totalCount) = await _teamRepository.SearchByMemberIdAsync(request.UserId, page, pageSize, cancellationToken);

        var items = teams.Select(TeamMapper.ToDto).ToList();

        return new PagedResult<TeamDto>(items, totalCount, page, pageSize);
    }
}
