using MediatR;
using TodoApp.Domain.Notifications;

namespace TodoApp.Application.Notifications.Commands.MarkNotificationRead;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly INotificationRepository _notificationRepository;

    public MarkNotificationReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new KeyNotFoundException("Notification not found.");

        notification.MarkAsRead();
        await _notificationRepository.UpdateAsync(notification, cancellationToken);
    }
}
