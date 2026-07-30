using MediatR;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.RemoveTeamMember;

public class RemoveTeamMemberCommandHandler : IRequestHandler<RemoveTeamMemberCommand>
{
    private readonly ITeamRepository _teamRepository;

    public RemoveTeamMemberCommandHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        // Team.RemoveMember already enforces: only the owner can remove
        // members, and the owner can't remove themselves (US-105 AC).
        team.RemoveMember(request.UserId, request.RequestingUserId);

        await _teamRepository.UpdateAsync(team, cancellationToken);

        // NOTE (US-105): "removed members are notified" — hook this up
        // once a notification mechanism exists (see EPIC-4 / US-118).
    }
}
