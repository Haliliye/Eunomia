using MediatR;
using TodoApp.Domain.Invitations;

namespace TodoApp.Application.Teams.Commands.DeclineInvitation;

public class DeclineInvitationCommandHandler : IRequestHandler<DeclineInvitationCommand>
{
    private readonly IInvitationRepository _invitationRepository;

    public DeclineInvitationCommandHandler(IInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
    }

    public async Task Handle(DeclineInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new KeyNotFoundException("Invitation not found.");

        invitation.Decline(request.RespondingUserId);

        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        // Deliberately no notification back to the inviter on decline — a
        // silent decline is the friendlier default; add one here if you want
        // the inviter to be told explicitly.
    }
}
