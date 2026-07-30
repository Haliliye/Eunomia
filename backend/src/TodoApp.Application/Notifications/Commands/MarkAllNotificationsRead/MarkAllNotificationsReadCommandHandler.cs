using MediatR;
using TodoApp.Domain.Notifications;

namespace TodoApp.Application.Notifications.Commands.MarkAllNotificationsRead;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly INotificationRepository _notificationRepository;

    public MarkAllNotificationsReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken) =>
        await _notificationRepository.MarkAllAsReadAsync(request.UserId, cancellationToken);
}
