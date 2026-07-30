using MediatR;
using TodoApp.Application.Notifications.DTOs;
using TodoApp.Domain.Notifications;

namespace TodoApp.Application.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetMyNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<IReadOnlyList<NotificationDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await _notificationRepository.GetByRecipientIdAsync(request.UserId, cancellationToken);

        return notifications
            .OrderByDescending(n => n.CreatedOn)
            .Select(n => new NotificationDto(n.Id, n.Type.ToString(), n.Message, n.RelatedEntityId, n.IsRead, n.CreatedOn))
            .ToList();
    }
}
