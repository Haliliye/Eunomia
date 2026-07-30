using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Notifications.DTOs;
using TodoApp.Domain.Comments;
using TodoApp.Domain.Notifications;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Application.Comments.EventHandlers;

/// <summary>
/// The actual US-114 side effect for mentions: creates a Notification row
/// (and pushes it live over SignalR) for each mentioned user. Triggered via
/// PublishDomainEventsAsync after AddCommentCommandHandler saves the comment.
/// </summary>
public class CommentAddedEventHandler : INotificationHandler<DomainEventNotification<CommentAddedEvent>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserStoryRepository _userStoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public CommentAddedEventHandler(
        INotificationRepository notificationRepository,
        IUserStoryRepository userStoryRepository,
        IUserRepository userRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _notificationRepository = notificationRepository;
        _userStoryRepository = userStoryRepository;
        _userRepository = userRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task Handle(DomainEventNotification<CommentAddedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        if (domainEvent.MentionedUserIds.Count == 0) return;

        var story = await _userStoryRepository.GetByIdAsync(domainEvent.UserStoryId, cancellationToken);
        var storyTitle = story?.Title ?? "a user story";

        foreach (var mentionedUserId in domainEvent.MentionedUserIds.Distinct())
        {
            if (mentionedUserId == domainEvent.AuthorId) continue; // don't notify yourself

            var mentionedUser = await _userRepository.GetByIdAsync(mentionedUserId, cancellationToken);
            if (mentionedUser is null || !mentionedUser.NotifyOnMention) continue;

            var mentionNotification = Notification.Create(
                id: Guid.NewGuid().ToString(),
                recipientUserId: mentionedUserId,
                type: NotificationType.Mention,
                message: $"{domainEvent.AuthorId} mentioned you in a comment on \"{storyTitle}\".",
                relatedEntityId: domainEvent.UserStoryId);

            await _notificationRepository.AddAsync(mentionNotification, cancellationToken);

            var dto = new NotificationDto(
                mentionNotification.Id, mentionNotification.Type.ToString(), mentionNotification.Message,
                mentionNotification.RelatedEntityId, mentionNotification.IsRead, mentionNotification.CreatedOn);

            await _realtimeNotifier.NotifyUserAsync(mentionedUserId, dto, cancellationToken);
        }
    }
}
