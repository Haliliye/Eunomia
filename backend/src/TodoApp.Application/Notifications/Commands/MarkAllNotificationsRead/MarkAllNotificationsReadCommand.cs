using MediatR;

namespace TodoApp.Application.Notifications.Commands.MarkAllNotificationsRead;

public record MarkAllNotificationsReadCommand(string UserId) : IRequest;
