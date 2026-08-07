using MediatR;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.ReorderBoardColumns;

public class ReorderBoardColumnsCommandHandler : IRequestHandler<ReorderBoardColumnsCommand>
{
    private readonly ITeamRepository _teamRepository;

    public ReorderBoardColumnsCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(ReorderBoardColumnsCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        team.ReorderColumns(request.OrderedColumnKeys, request.RequestingUserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
    }
}
