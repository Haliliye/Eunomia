using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Notifications.DTOs;
using TodoApp.Domain.Invitations;
using TodoApp.Domain.Notifications;
using TodoApp.Domain.Teams;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Teams.EventHandlers;

/// <summary>
/// The actual side effect for a new invitation: creates the Notification row
/// (RelatedEntityId = the invitation's id, so the frontend can offer
/// accept/decline actions right on the notification) and pushes it live.
/// </summary>
public class InvitationCreatedEventHandler : INotificationHandler<DomainEventNotification<InvitationCreatedEvent>>
{
    private readonly ITeamRepository _teamRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IUserRepository _userRepository;

    public InvitationCreatedEventHandler(
        ITeamRepository teamRepository,
        INotificationRepository notificationRepository,
        IRealtimeNotifier realtimeNotifier,
        IUserRepository userRepository)
    {
        _teamRepository = teamRepository;
        _notificationRepository = notificationRepository;
        _realtimeNotifier = realtimeNotifier;
        _userRepository = userRepository;
    }

    public async Task Handle(DomainEventNotification<InvitationCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        // Even if this notification is skipped, the invitation itself still
        // exists and shows up in the "Pending invitations" panel on /teams —
        // turning this preference off only silences the bell, it never hides
        // an invitation the person still needs to act on.
        var invitedUser = await _userRepository.GetByIdAsync(domainEvent.InvitedUserId, cancellationToken);
        if (invitedUser is null || !invitedUser.NotifyOnInvitation) return;

        var team = await _teamRepository.GetByIdAsync(domainEvent.TeamId, cancellationToken);
        var teamName = team?.Name ?? "a team";

        var invitationNotification = Notification.Create(
            id: Guid.NewGuid().ToString(),
            recipientUserId: domainEvent.InvitedUserId,
            type: NotificationType.TeamInvitation,
            message: $"You've been invited to join \"{teamName}\".",
            relatedEntityId: domainEvent.InvitationId);

        await _notificationRepository.AddAsync(invitationNotification, cancellationToken);

        var dto = new NotificationDto(
            invitationNotification.Id, invitationNotification.Type.ToString(), invitationNotification.Message,
            invitationNotification.RelatedEntityId, invitationNotification.IsRead, invitationNotification.CreatedOn);

        await _realtimeNotifier.NotifyUserAsync(domainEvent.InvitedUserId, dto, cancellationToken);
    }
}
