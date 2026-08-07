using MediatR;
using TodoApp.Application.Boards.DTOs;
using TodoApp.Domain.Boards;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Boards.Queries.GetBoardsByTeam;

public class GetBoardsByTeamQueryHandler : IRequestHandler<GetBoardsByTeamQuery, IReadOnlyList<BoardDto>>
{
    private readonly IBoardRepository _boardRepository;
    private readonly ITeamRepository _teamRepository;

    public GetBoardsByTeamQueryHandler(IBoardRepository boardRepository, ITeamRepository teamRepository)
    {
        _boardRepository = boardRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<BoardDto>> Handle(GetBoardsByTeamQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var boards = await _boardRepository.GetByTeamIdAsync(request.TeamId, cancellationToken);
        return boards.Select(b => new BoardDto(b.Id, b.TeamId, b.Name, b.SprintId, b.CreatedOn)).ToList();
    }
}
