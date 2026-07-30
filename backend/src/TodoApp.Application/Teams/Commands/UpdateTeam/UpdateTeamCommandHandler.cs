using MediatR;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.UpdateTeam;

public class UpdateTeamCommandHandler : IRequestHandler<UpdateTeamCommand>
{
    private readonly ITeamRepository _teamRepository;

    public UpdateTeamCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        team.UpdateDetails(request.Name, request.Description, request.RequestingUserId);

        await _teamRepository.UpdateAsync(team, cancellationToken);
    }
}
