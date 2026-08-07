using MediatR;
using TodoApp.Application.Boards.DTOs;
using TodoApp.Domain.Boards;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Boards.Commands.CreateBoard;

public class CreateBoardCommandHandler : IRequestHandler<CreateBoardCommand, BoardDto>
{
    private readonly IBoardRepository _boardRepository;
    private readonly ITeamRepository _teamRepository;

    public CreateBoardCommandHandler(IBoardRepository boardRepository, ITeamRepository teamRepository)
    {
        _boardRepository = boardRepository;
        _teamRepository = teamRepository;
    }

    public async Task<BoardDto> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var board = Board.Create(Guid.NewGuid().ToString(), request.TeamId, request.Name, request.SprintId);
        await _boardRepository.AddAsync(board, cancellationToken);

        return new BoardDto(board.Id, board.TeamId, board.Name, board.SprintId, board.CreatedOn);
    }
}
