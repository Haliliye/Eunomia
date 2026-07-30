using MediatR;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.SetMemberRole;

public class SetMemberRoleCommandHandler : IRequestHandler<SetMemberRoleCommand>
{
    private readonly ITeamRepository _teamRepository;

    public SetMemberRoleCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(SetMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        if (!Enum.TryParse<TeamRole>(request.NewRole, out var role) || role == TeamRole.Owner)
            throw new ArgumentException($"Invalid role '{request.NewRole}'.");

        team.SetMemberRole(request.UserId, role, request.RequestingUserId);
        await _teamRepository.UpdateAsync(team, cancellationToken);
    }
}
