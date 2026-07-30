using TodoApp.Domain.Common;

namespace TodoApp.Domain.Auth;

/// <summary>Same pattern as RefreshToken/PasswordResetToken — hashed, one-time-use, TTL-expired.</summary>
public class EmailVerificationToken : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresOn { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UsedOn { get; private set; }

    public bool IsActive => UsedOn is null && DateTime.UtcNow < ExpiresOn;

    private EmailVerificationToken() { }

    private EmailVerificationToken(string id, string userId, string tokenHash, DateTime expiresOn) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresOn = expiresOn;
        CreatedOn = DateTime.UtcNow;
    }

    public static EmailVerificationToken Create(string id, string userId, string tokenHash, DateTime expiresOn) =>
        new(id, userId, tokenHash, expiresOn);

    public static EmailVerificationToken Rehydrate(string id, string userId, string tokenHash, DateTime expiresOn, DateTime createdOn, DateTime? usedOn)
    {
        var token = new EmailVerificationToken(id, userId, tokenHash, expiresOn) { CreatedOn = createdOn, UsedOn = usedOn };
        return token;
    }

    public void MarkUsed() => UsedOn ??= DateTime.UtcNow;
}
