namespace TodoApp.Application.UserStories.DTOs;

public record AttachmentDto(string Id, string FileName, string ContentType, long SizeBytes, string UploadedByUserId, DateTime UploadedOn);
