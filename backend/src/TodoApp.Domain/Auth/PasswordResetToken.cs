using TodoApp.Domain.Common;

namespace TodoApp.Domain.Auth;

/// <summary>
/// A one-time-use, short-lived token for resetting a forgotten password.
/// Stored hashed, same principle as RefreshToken — the raw value only ever
/// exists in the (would-be) email and briefly in memory.
/// </summary>
public class PasswordResetToken : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresOn { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UsedOn { get; private set; }

    public bool IsActive => UsedOn is null && DateTime.UtcNow < ExpiresOn;

    private PasswordResetToken() { }

    private PasswordResetToken(string id, string userId, string tokenHash, DateTime expiresOn) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresOn = expiresOn;
        CreatedOn = DateTime.UtcNow;
    }

    public static PasswordResetToken Create(string id, string userId, string tokenHash, DateTime expiresOn) =>
        new(id, userId, tokenHash, expiresOn);

    public static PasswordResetToken Rehydrate(string id, string userId, string tokenHash, DateTime expiresOn, DateTime createdOn, DateTime? usedOn)
    {
        var token = new PasswordResetToken(id, userId, tokenHash, expiresOn) { CreatedOn = createdOn, UsedOn = usedOn };
        return token;
    }

    public void MarkUsed() => UsedOn ??= DateTime.UtcNow;
}
