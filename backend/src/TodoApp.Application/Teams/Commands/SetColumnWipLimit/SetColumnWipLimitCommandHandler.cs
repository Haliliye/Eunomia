using MediatR;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.SetColumnWipLimit;

public class SetColumnWipLimitCommandHandler : IRequestHandler<SetColumnWipLimitCommand>
{
    private readonly ITeamRepository _teamRepository;

    public SetColumnWipLimitCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(SetColumnWipLimitCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        team.SetColumnWipLimit(request.Status, request.Limit, request.RequestingUserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
    }
}
