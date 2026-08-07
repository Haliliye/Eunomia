using MediatR;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.RenameBoardColumn;

public class RenameBoardColumnCommandHandler : IRequestHandler<RenameBoardColumnCommand>
{
    private readonly ITeamRepository _teamRepository;

    public RenameBoardColumnCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(RenameBoardColumnCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        team.RenameColumn(request.ColumnKey, request.Name, request.RequestingUserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
    }
}
