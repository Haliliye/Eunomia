using MongoDB.Bson.Serialization.Attributes;

namespace TodoApp.Infrastructure.Persistence.Documents;

public class NotificationDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string RecipientUserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RelatedEntityId { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedOn { get; set; }
}
