using MediatR;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Queries.GetTeamById;

public class GetTeamByIdQueryHandler : IRequestHandler<GetTeamByIdQuery, TeamDto?>
{
    private readonly ITeamRepository _teamRepository;

    public GetTeamByIdQueryHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<TeamDto?> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null) return null;

        // Previously missing entirely — any authenticated user could view any
        // team's full details (members, labels, etc.) just by knowing/guessing
        // its id. Only members get to see it.
        team.EnsureIsMember(request.RequestingUserId);

        return TeamMapper.ToDto(team);
    }
}
