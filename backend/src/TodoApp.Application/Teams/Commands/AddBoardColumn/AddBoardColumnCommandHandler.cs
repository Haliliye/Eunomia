using MediatR;
using TodoApp.Application.Teams.DTOs;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.AddBoardColumn;

public class AddBoardColumnCommandHandler : IRequestHandler<AddBoardColumnCommand, BoardColumnDto>
{
    private readonly ITeamRepository _teamRepository;

    public AddBoardColumnCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<BoardColumnDto> Handle(AddBoardColumnCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        var column = team.AddColumn(request.Name, request.RequestingUserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);

        return new BoardColumnDto(column.Key, column.Name, column.Order);
    }
}
