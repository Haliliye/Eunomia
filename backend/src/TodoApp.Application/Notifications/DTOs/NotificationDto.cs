namespace TodoApp.Application.Notifications.DTOs;

public record NotificationDto(
    string Id,
    string Type,
    string Message,
    string RelatedEntityId,
    bool IsRead,
    DateTime CreatedOn);
