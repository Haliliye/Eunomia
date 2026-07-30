using MediatR;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Teams.Commands.CancelInvitation;

public class CancelInvitationCommandHandler : IRequestHandler<CancelInvitationCommand>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly ITeamRepository _teamRepository;

    public CancelInvitationCommandHandler(IInvitationRepository invitationRepository, ITeamRepository teamRepository)
    {
        _invitationRepository = invitationRepository;
        _teamRepository = teamRepository;
    }

    public async Task Handle(CancelInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new KeyNotFoundException("Invitation not found.");

        // Either whoever sent it, or the team's current owner, can withdraw
        // a still-pending invitation.
        var isInviter = invitation.InvitedByUserId == request.RequestingUserId;
        if (!isInviter)
        {
            var team = await _teamRepository.GetByIdAsync(invitation.TeamId, cancellationToken)
                ?? throw new KeyNotFoundException("Team not found.");

            var isOwner = team.Members.Any(m => m.UserId == request.RequestingUserId && m.Role == TeamRole.Owner);
            if (!isOwner)
                throw new UnauthorizedAccessException("Only the inviter or the team owner can cancel this invitation.");
        }

        invitation.Cancel();
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
    }
}
