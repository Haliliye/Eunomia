using TodoApp.Domain.Common;

namespace TodoApp.Domain.Comments;

/// <summary>
/// Aggregate root for a single comment on a user story (EPIC-4). Lives in
/// its own collection referencing UserStoryId — see the note on UserStory
/// for why comments aren't embedded in the story itself.
/// </summary>
public class Comment : AggregateRoot
{
    public string UserStoryId { get; private set; } = string.Empty;
    public string AuthorId { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public IReadOnlyCollection<string> MentionedUserIds { get; private set; } = Array.Empty<string>();
    public DateTime CreatedOn { get; private set; }
    public DateTime? EditedOn { get; private set; }

    private Comment() { }

    private Comment(string id, string userStoryId, string authorId, string content, IEnumerable<string> mentionedUserIds)
        : base(id)
    {
        UserStoryId = userStoryId;
        AuthorId = authorId;
        Content = content;
        MentionedUserIds = mentionedUserIds.ToList();
        CreatedOn = DateTime.UtcNow;
    }

    public static Comment Create(string id, string userStoryId, string authorId, string content, IEnumerable<string> mentionedUserIds)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment cannot be empty.", nameof(content));

        var comment = new Comment(id, userStoryId, authorId, content.Trim(), mentionedUserIds);

        // US-114 AC: mentioned members are notified — raised here so the
        // Application layer's CommentAddedEventHandler creates the actual
        // Notification rows, keeping that side-effect out of the command handler.
        comment.RaiseDomainEvent(new CommentAddedEvent(comment.Id, comment.UserStoryId, comment.AuthorId, comment.MentionedUserIds));

        return comment;
    }

    public static Comment Rehydrate(
        string id, string userStoryId, string authorId, string content,
        IEnumerable<string> mentionedUserIds, DateTime createdOn, DateTime? editedOn)
    {
        var comment = new Comment(id, userStoryId, authorId, content, mentionedUserIds)
        {
            CreatedOn = createdOn,
            EditedOn = editedOn
        };
        return comment;
    }

    /// <summary>Only the author may edit — enforced by the caller (AuthorId comparison) before this is called; kept here too as a guard against calling it on someone else's comment by mistake.</summary>
    public void UpdateContent(string content, IEnumerable<string> mentionedUserIds)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment cannot be empty.", nameof(content));

        Content = content.Trim();
        MentionedUserIds = mentionedUserIds.ToList();
        EditedOn = DateTime.UtcNow;

        // NOTE: editing doesn't re-notify newly added mentions — if you want
        // "mentioning someone in an edit notifies them too", raise a
        // CommentAddedEvent-like event here for the newly added ids only.
    }
}
