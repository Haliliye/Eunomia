using MongoDB.Bson.Serialization.Attributes;

namespace TodoApp.Infrastructure.Persistence.Documents;

public class UserDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }

    // Nullable so accounts created before this feature existed (missing this
    // field in Mongo) fall back to "true" — see UserRepository.ToDomain.
    public bool? NotifyOnAssignment { get; set; }
    public bool? NotifyOnMention { get; set; }
    public bool? NotifyOnInvitation { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool? NotifyOnDueSoon { get; set; }
    public int? ReminderLeadTimeHours { get; set; }
}
