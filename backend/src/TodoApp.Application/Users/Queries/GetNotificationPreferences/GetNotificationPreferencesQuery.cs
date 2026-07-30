using MediatR;
using TodoApp.Application.Users.DTOs;

namespace TodoApp.Application.Users.Queries.GetNotificationPreferences;

public record GetNotificationPreferencesQuery(string UserId) : IRequest<NotificationPreferencesDto>;
