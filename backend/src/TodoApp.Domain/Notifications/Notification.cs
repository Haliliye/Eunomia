using TodoApp.Domain.Common;

namespace TodoApp.Domain.Notifications;

/// <summary>
/// Aggregate root for an in-app notification (US-118). RelatedEntityId
/// points at whatever the notification is about (a user story id for
/// assignment/mention notifications) so the frontend can link to it.
/// </summary>
public class Notification : AggregateRoot
{
    public string RecipientUserId { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string RelatedEntityId { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime CreatedOn { get; private set; }

    private Notification() { }

    private Notification(string id, string recipientUserId, NotificationType type, string message, string relatedEntityId)
        : base(id)
    {
        RecipientUserId = recipientUserId;
        Type = type;
        Message = message;
        RelatedEntityId = relatedEntityId;
        IsRead = false;
        CreatedOn = DateTime.UtcNow;
    }

    public static Notification Create(string id, string recipientUserId, NotificationType type, string message, string relatedEntityId) =>
        new(id, recipientUserId, type, message, relatedEntityId);

    public static Notification Rehydrate(
        string id, string recipientUserId, NotificationType type, string message,
        string relatedEntityId, bool isRead, DateTime createdOn)
    {
        var notification = new Notification(id, recipientUserId, type, message, relatedEntityId)
        {
            IsRead = isRead,
            CreatedOn = createdOn
        };
        return notification;
    }

    public void MarkAsRead() => IsRead = true;
}
