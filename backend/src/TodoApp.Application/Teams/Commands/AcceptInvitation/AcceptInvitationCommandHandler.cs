using MediatR;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Notifications;
using TodoApp.Domain.Teams;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Teams.Commands.AcceptInvitation;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;

    public AcceptInvitationCommandHandler(
        IInvitationRepository invitationRepository,
        ITeamRepository teamRepository,
        INotificationRepository notificationRepository,
        IUserRepository userRepository)
    {
        _invitationRepository = invitationRepository;
        _teamRepository = teamRepository;
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
    }

    public async Task Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new KeyNotFoundException("Invitation not found.");

        // Accept() throws UnauthorizedAccessException if RespondingUserId isn't
        // the invitee, and InvalidOperationException if it's not Pending anymore.
        invitation.Accept(request.RespondingUserId);

        var team = await _teamRepository.GetByIdAsync(invitation.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        team.AddMemberFromInvitation(request.RespondingUserId);

        await _teamRepository.UpdateAsync(team, cancellationToken);
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        // Let the inviter know their invitation was accepted — unless they've
        // turned invitation notifications off.
        var inviter = await _userRepository.GetByIdAsync(invitation.InvitedByUserId, cancellationToken);
        if (inviter is not null && inviter.NotifyOnInvitation)
        {
            var notification = Notification.Create(
                id: Guid.NewGuid().ToString(),
                recipientUserId: invitation.InvitedByUserId,
                type: NotificationType.InvitationAccepted,
                message: $"Your invitation to \"{team.Name}\" was accepted.",
                relatedEntityId: team.Id);

            await _notificationRepository.AddAsync(notification, cancellationToken);
        }
    }
}
