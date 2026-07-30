namespace TodoApp.Domain.Notifications;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetByRecipientIdAsync(string recipientUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(string recipientUserId, CancellationToken cancellationToken = default);
}
