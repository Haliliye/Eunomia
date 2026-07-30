using TodoApp.Domain.Common;

namespace TodoApp.Domain.Auth;

/// <summary>
/// A long-lived refresh token, stored as a hash (never the raw value — same
/// principle as password hashing). One user can have several of these at
/// once (one per device/browser they're logged in on), unlike storing a
/// single token directly on User, which would log every other device out
/// on each new login.
/// </summary>
public class RefreshToken : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresOn { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? RevokedOn { get; private set; }

    public bool IsActive => RevokedOn is null && DateTime.UtcNow < ExpiresOn;

    private RefreshToken() { }

    private RefreshToken(string id, string userId, string tokenHash, DateTime expiresOn) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresOn = expiresOn;
        CreatedOn = DateTime.UtcNow;
    }

    public static RefreshToken Create(string id, string userId, string tokenHash, DateTime expiresOn) =>
        new(id, userId, tokenHash, expiresOn);

    public static RefreshToken Rehydrate(string id, string userId, string tokenHash, DateTime expiresOn, DateTime createdOn, DateTime? revokedOn)
    {
        var token = new RefreshToken(id, userId, tokenHash, expiresOn) { CreatedOn = createdOn, RevokedOn = revokedOn };
        return token;
    }

    public void Revoke() => RevokedOn ??= DateTime.UtcNow;
}
