namespace TodoApp.Application.Comments.DTOs;

public record CommentDto(
    string Id,
    string UserStoryId,
    string AuthorId,
    string Content,
    IReadOnlyCollection<string> MentionedUserIds,
    DateTime CreatedOn,
    DateTime? EditedOn);
