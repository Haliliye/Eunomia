using MediatR;
using TodoApp.Application.Notifications.DTOs;

namespace TodoApp.Application.Notifications.Queries.GetMyNotifications;

public record GetMyNotificationsQuery(string UserId) : IRequest<IReadOnlyList<NotificationDto>>;
