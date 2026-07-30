using TodoApp.Application.Common;
using TodoApp.Application.Notifications.DTOs;
using TodoApp.Domain.Notifications;
using TodoApp.Domain.UserStories;
using TodoApp.Domain.Users;

namespace TodoApp.Api.BackgroundServices;

/// <summary>
/// US-120: periodically checks for stories due soon and reminds the assignee.
/// Runs as a singleton hosted service but creates a fresh DI scope per check
/// (repositories/IRealtimeNotifier are scoped, this service is not).
/// </summary>
public class DueDateReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DueDateReminderBackgroundService> _logger;

    public DueDateReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<DueDateReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // A single failed cycle (e.g. a transient Mongo hiccup) shouldn't
                // kill the whole background service — log and try again next interval.
                _logger.LogError(ex, "Due-date reminder check failed.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckAndSendRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var storyRepository = scope.ServiceProvider.GetRequiredService<IUserStoryRepository>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var realtimeNotifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();

        var candidates = await storyRepository.GetPendingReminderCandidatesAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var story in candidates)
        {
            if (story.AssigneeId is null || story.DueDate is null) continue;

            var assignee = await userRepository.GetByIdAsync(story.AssigneeId, cancellationToken);
            if (assignee is null || !assignee.NotifyOnDueSoon) continue;

            var reminderThreshold = story.DueDate.Value.AddHours(-assignee.ReminderLeadTimeHours);

            // Not yet within the assignee's configured lead-time window.
            if (now < reminderThreshold) continue;

            // Already overdue — the point of a "reminder" is to prevent missing
            // the deadline, so once it's passed there's nothing left to remind.
            if (now >= story.DueDate.Value) continue;

            var notification = Notification.Create(
                Guid.NewGuid().ToString(), assignee.Id, NotificationType.DueSoon,
                $"\"{story.Title}\" is due {story.DueDate.Value:MMM d} — coming up soon.", story.Id);

            await notificationRepository.AddAsync(notification, cancellationToken);

            var dto = new NotificationDto(
                notification.Id, notification.Type.ToString(), notification.Message,
                notification.RelatedEntityId, notification.IsRead, notification.CreatedOn);
            await realtimeNotifier.NotifyUserAsync(assignee.Id, dto, cancellationToken);

            story.MarkReminderSent();
            await storyRepository.UpdateAsync(story, cancellationToken);
        }
    }
}
