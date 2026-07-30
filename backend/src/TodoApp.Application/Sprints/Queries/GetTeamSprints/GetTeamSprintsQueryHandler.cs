using MediatR;
using TodoApp.Application.Sprints.DTOs;
using TodoApp.Domain.Sprints;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Sprints.Queries.GetTeamSprints;

public class GetTeamSprintsQueryHandler : IRequestHandler<GetTeamSprintsQuery, IReadOnlyList<SprintDto>>
{
    private readonly ISprintRepository _sprintRepository;
    private readonly ITeamRepository _teamRepository;

    public GetTeamSprintsQueryHandler(ISprintRepository sprintRepository, ITeamRepository teamRepository)
    {
        _sprintRepository = sprintRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<SprintDto>> Handle(GetTeamSprintsQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var sprints = await _sprintRepository.GetByTeamIdAsync(request.TeamId, cancellationToken);

        return sprints
            .OrderByDescending(s => s.CreatedOn)
            .Select(s => new SprintDto(s.Id, s.TeamId, s.Name, s.StartDate, s.EndDate, s.Status.ToString(), s.CreatedOn))
            .ToList();
    }
}
