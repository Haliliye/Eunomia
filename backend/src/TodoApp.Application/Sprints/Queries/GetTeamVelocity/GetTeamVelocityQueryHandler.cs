using MediatR;
using TodoApp.Domain.Sprints;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Sprints.Queries.GetTeamVelocity;

public class GetTeamVelocityQueryHandler : IRequestHandler<GetTeamVelocityQuery, IReadOnlyList<VelocityPointDto>>
{
    private readonly ISprintRepository _sprintRepository;
    private readonly ITeamRepository _teamRepository;

    public GetTeamVelocityQueryHandler(ISprintRepository sprintRepository, ITeamRepository teamRepository)
    {
        _sprintRepository = sprintRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<VelocityPointDto>> Handle(GetTeamVelocityQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var sprints = await _sprintRepository.GetByTeamIdAsync(request.TeamId, cancellationToken);

        return sprints
            .Where(s => s.Status == SprintStatus.Completed && s.CompletedPointsAtCompletion.HasValue)
            .OrderBy(s => s.EndDate)
            .Select(s => new VelocityPointDto(s.Id, s.Name, s.EndDate, s.TotalPointsAtStart, s.CompletedPointsAtCompletion!.Value))
            .ToList();
    }
}
