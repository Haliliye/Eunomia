namespace TodoApp.Application.Activities.DTOs;

public record ActivityDto(string Id, string ActorUserId, string Type, string Message, string? RelatedEntityId, DateTime CreatedOn);
