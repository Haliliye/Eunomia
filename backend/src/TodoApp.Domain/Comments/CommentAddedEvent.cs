using TodoApp.Domain.Common;

namespace TodoApp.Domain.Comments;

public sealed class CommentAddedEvent : IDomainEvent
{
    public string CommentId { get; }
    public string UserStoryId { get; }
    public string AuthorId { get; }
    public IReadOnlyCollection<string> MentionedUserIds { get; }
    public DateTime OccurredOn { get; }

    public CommentAddedEvent(string commentId, string userStoryId, string authorId, IReadOnlyCollection<string> mentionedUserIds)
    {
        CommentId = commentId;
        UserStoryId = userStoryId;
        AuthorId = authorId;
        MentionedUserIds = mentionedUserIds;
        OccurredOn = DateTime.UtcNow;
    }
}
