using MongoDB.Driver;
using TodoApp.Domain.Notifications;
using TodoApp.Infrastructure.Persistence.Documents;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly IMongoCollection<NotificationDocument> _notifications;

    public NotificationRepository(MongoDbContext context)
    {
        _notifications = context.Notifications;
    }

    public async Task<Notification?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await _notifications.Find(n => n.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyList<Notification>> GetByRecipientIdAsync(string recipientUserId, CancellationToken cancellationToken = default)
    {
        var documents = await _notifications.Find(n => n.RecipientUserId == recipientUserId).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToList();
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default) =>
        await _notifications.InsertOneAsync(ToDocument(notification), cancellationToken: cancellationToken);

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default) =>
        await _notifications.ReplaceOneAsync(n => n.Id == notification.Id, ToDocument(notification), cancellationToken: cancellationToken);

    public async Task MarkAllAsReadAsync(string recipientUserId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationDocument>.Filter.Eq(n => n.RecipientUserId, recipientUserId);
        var update = Builders<NotificationDocument>.Update.Set(n => n.IsRead, true);
        await _notifications.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }

    private static NotificationDocument ToDocument(Notification notification) => new()
    {
        Id = notification.Id,
        RecipientUserId = notification.RecipientUserId,
        Type = notification.Type.ToString(),
        Message = notification.Message,
        RelatedEntityId = notification.RelatedEntityId,
        IsRead = notification.IsRead,
        CreatedOn = notification.CreatedOn
    };

    private static Notification ToDomain(NotificationDocument document) => Notification.Rehydrate(
        document.Id, document.RecipientUserId, Enum.Parse<NotificationType>(document.Type),
        document.Message, document.RelatedEntityId, document.IsRead, document.CreatedOn);
}
