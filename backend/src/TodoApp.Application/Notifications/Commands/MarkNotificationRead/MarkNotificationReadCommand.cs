using MediatR;

namespace TodoApp.Application.Notifications.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(string NotificationId, string RequestingUserId) : IRequest;
