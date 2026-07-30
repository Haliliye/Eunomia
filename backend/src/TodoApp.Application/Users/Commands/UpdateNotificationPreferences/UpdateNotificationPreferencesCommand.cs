using MediatR;

namespace TodoApp.Application.Users.Commands.UpdateNotificationPreferences;

public record UpdateNotificationPreferencesCommand(string UserId, bool NotifyOnAssignment, bool NotifyOnMention, bool NotifyOnInvitation, bool NotifyOnDueSoon, int ReminderLeadTimeHours) : IRequest;
