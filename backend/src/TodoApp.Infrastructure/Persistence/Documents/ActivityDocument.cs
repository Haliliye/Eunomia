using MongoDB.Bson.Serialization.Attributes;

namespace TodoApp.Infrastructure.Persistence.Documents;

public class ActivityDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RelatedEntityId { get; set; }
    public DateTime CreatedOn { get; set; }
}
