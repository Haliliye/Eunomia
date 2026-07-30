using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Notifications.DTOs;
using TodoApp.Domain.Notifications;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Application.UserStories.EventHandlers;

/// <summary>
/// The actual US-118 side effect for assignment: creates the Notification row
/// and pushes it live over SignalR. Triggered via
/// DomainEventDispatchExtensions.PublishDomainEventsAsync after
/// AssignUserStoryCommandHandler saves the story — the command handler no
/// longer knows anything about notifications.
/// </summary>
public class UserStoryAssignedEventHandler : INotificationHandler<DomainEventNotification<UserStoryAssignedEvent>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IUserRepository _userRepository;

    public UserStoryAssignedEventHandler(INotificationRepository notificationRepository, IRealtimeNotifier realtimeNotifier, IUserRepository userRepository)
    {
        _notificationRepository = notificationRepository;
        _realtimeNotifier = realtimeNotifier;
        _userRepository = userRepository;
    }

    public async Task Handle(DomainEventNotification<UserStoryAssignedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        // Respect the assignee's own notification preferences — assigning the
        // story still happens either way, only the notification is skipped.
        var assignee = await _userRepository.GetByIdAsync(domainEvent.AssigneeId, cancellationToken);
        if (assignee is null || !assignee.NotifyOnAssignment) return;

        var userStoryNotification = Notification.Create(
            id: Guid.NewGuid().ToString(),
            recipientUserId: domainEvent.AssigneeId,
            type: NotificationType.Assignment,
            message: $"You were assigned to \"{domainEvent.Title}\".",
            relatedEntityId: domainEvent.UserStoryId);

        await _notificationRepository.AddAsync(userStoryNotification, cancellationToken);

        var dto = new NotificationDto(
            userStoryNotification.Id, userStoryNotification.Type.ToString(), userStoryNotification.Message,
            userStoryNotification.RelatedEntityId, userStoryNotification.IsRead, userStoryNotification.CreatedOn);

        await _realtimeNotifier.NotifyUserAsync(domainEvent.AssigneeId, dto, cancellationToken);
    }
}
