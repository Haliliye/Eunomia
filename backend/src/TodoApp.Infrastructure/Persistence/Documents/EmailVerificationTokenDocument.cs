using MongoDB.Bson.Serialization.Attributes;

namespace TodoApp.Infrastructure.Persistence.Documents;

public class EmailVerificationTokenDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UsedOn { get; set; }
}
