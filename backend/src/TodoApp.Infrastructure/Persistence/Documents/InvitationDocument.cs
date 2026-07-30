using MongoDB.Bson.Serialization.Attributes;

namespace TodoApp.Infrastructure.Persistence.Documents;

public class InvitationDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string InvitedUserId { get; set; } = string.Empty;
    public string InvitedByUserId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public DateTime? RespondedOn { get; set; }
}
