using MediatR;
using TodoApp.Domain.Boards;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Boards.Commands.DeleteBoard;

public class DeleteBoardCommandHandler : IRequestHandler<DeleteBoardCommand>
{
    private readonly IBoardRepository _boardRepository;
    private readonly ITeamRepository _teamRepository;

    public DeleteBoardCommandHandler(IBoardRepository boardRepository, ITeamRepository teamRepository)
    {
        _boardRepository = boardRepository;
        _teamRepository = teamRepository;
    }

    public async Task Handle(DeleteBoardCommand request, CancellationToken cancellationToken)
    {
        var board = await _boardRepository.GetByIdAsync(request.BoardId, cancellationToken)
            ?? throw new KeyNotFoundException("Board not found.");

        var team = await _teamRepository.GetByIdAsync(board.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        await _boardRepository.DeleteAsync(request.BoardId, cancellationToken);
    }
}
