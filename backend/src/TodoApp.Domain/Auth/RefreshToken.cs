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

    /// <summary>
    /// Shared by every token descended from the same original login via
    /// rotation — a fresh login gets a new FamilyId (its own Id), and each
    /// rotation carries the family forward instead of starting a new one.
    /// This is what makes reuse detection possible: if a token that's
    /// already been rotated away gets presented again (the old, revoked one
    /// — evidence someone else has a copy), every token sharing its
    /// FamilyId can be revoked at once, ending the compromised session on
    /// every device instead of just the one that happened to ask next.
    /// </summary>
    public string FamilyId { get; private set; } = string.Empty;

    public bool IsActive => RevokedOn is null && DateTime.UtcNow < ExpiresOn;

    private RefreshToken() { }

    private RefreshToken(string id, string userId, string tokenHash, DateTime expiresOn, string familyId) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresOn = expiresOn;
        FamilyId = familyId;
        CreatedOn = DateTime.UtcNow;
    }

    /// <summary>A brand-new login — starts its own family (FamilyId defaults to this token's own Id when none is given).</summary>
    public static RefreshToken Create(string id, string userId, string tokenHash, DateTime expiresOn, string? familyId = null) =>
        new(id, userId, tokenHash, expiresOn, familyId ?? id);

    public static RefreshToken Rehydrate(string id, string userId, string tokenHash, DateTime expiresOn, DateTime createdOn, DateTime? revokedOn, string familyId)
    {
        var token = new RefreshToken(id, userId, tokenHash, expiresOn, familyId) { CreatedOn = createdOn, RevokedOn = revokedOn };
        return token;
    }

    public void Revoke() => RevokedOn ??= DateTime.UtcNow;
}
