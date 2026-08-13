namespace TodoApp.Domain.Auth;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>Revokes every active token sharing the given FamilyId — the reuse-detection response (see RefreshToken.FamilyId): one compromised token ends every session descended from the same login, not just the one caller.</summary>
    Task RevokeAllInFamilyAsync(string familyId, CancellationToken cancellationToken = default);
}
