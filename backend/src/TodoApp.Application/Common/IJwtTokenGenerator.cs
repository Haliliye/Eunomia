using TodoApp.Domain.Users;

namespace TodoApp.Application.Common;

/// <summary>Abstraction so Application doesn't depend on a specific JWT library (implemented in Infrastructure).</summary>
public interface IJwtTokenGenerator
{
    string GenerateToken(User user);

    /// <summary>Raw (unhashed) refresh token + its expiry — Application hashes it (TokenHasher) before persisting; this keeps the expiry duration configured in Infrastructure (JwtSettings) without Application reading that config directly.</summary>
    (string Token, DateTime ExpiresOn) GenerateRefreshToken();
}
