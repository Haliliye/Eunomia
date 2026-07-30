namespace TodoApp.Application.Users.DTOs;

public record NotificationPreferencesDto(bool NotifyOnAssignment, bool NotifyOnMention, bool NotifyOnInvitation, bool NotifyOnDueSoon, int ReminderLeadTimeHours);
