using MediatR;
using TodoApp.Domain.Boards;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Boards.Commands.RenameBoard;

public class RenameBoardCommandHandler : IRequestHandler<RenameBoardCommand>
{
    private readonly IBoardRepository _boardRepository;
    private readonly ITeamRepository _teamRepository;

    public RenameBoardCommandHandler(IBoardRepository boardRepository, ITeamRepository teamRepository)
    {
        _boardRepository = boardRepository;
        _teamRepository = teamRepository;
    }

    public async Task Handle(RenameBoardCommand request, CancellationToken cancellationToken)
    {
        var board = await _boardRepository.GetByIdAsync(request.BoardId, cancellationToken)
            ?? throw new KeyNotFoundException("Board not found.");

        var team = await _teamRepository.GetByIdAsync(board.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        board.Rename(request.Name);
        board.SetSprint(request.SprintId);

        await _boardRepository.UpdateAsync(board, cancellationToken);
    }
}
