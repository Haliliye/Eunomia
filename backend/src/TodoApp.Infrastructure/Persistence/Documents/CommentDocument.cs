using MongoDB.Bson.Serialization.Attributes;

namespace TodoApp.Infrastructure.Persistence.Documents;

public class CommentDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string UserStoryId { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> MentionedUserIds { get; set; } = new();
    public DateTime CreatedOn { get; set; }
    public DateTime? EditedOn { get; set; }
}
