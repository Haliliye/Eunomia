namespace TodoApp.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Access token lifetime — kept short since it can't be revoked before it expires.</summary>
    public int ExpiryMinutes { get; set; } = 15;

    /// <summary>Refresh token lifetime — long-lived, but unlike the access token it IS revocable (see RefreshToken.Revoke).</summary>
    public int RefreshTokenExpiryDays { get; set; } = 30;
}
