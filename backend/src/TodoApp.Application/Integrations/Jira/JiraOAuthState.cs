namespace TodoApp.Application.Integrations.Jira;

/// <summary>
/// Packs/unpacks the plaintext payload that goes inside the encrypted OAuth
/// "state" parameter (see StartJiraConnectionCommandHandler / CompleteJiraConnectionCommandHandler).
/// Kept separate from the encryption itself (ITokenCipher) so this class is
/// just plain string formatting — easy to reason about and test on its own.
/// </summary>
internal static class JiraOAuthState
{
    private const int MaxAgeMinutes = 10;

    public static string Protect(string userId) => $"{userId}|{DateTime.UtcNow.Ticks}";

    /// <summary>Returns the userId if the payload is well-formed and not expired; null otherwise (caller should treat that as an invalid/expired authorization attempt).</summary>
    public static string? TryUnprotect(string payload)
    {
        var parts = payload.Split('|', 2);
        if (parts.Length != 2) return null;

        var userId = parts[0];
        if (string.IsNullOrWhiteSpace(userId) || !long.TryParse(parts[1], out var ticks)) return null;

        var issuedOn = new DateTime(ticks, DateTimeKind.Utc);
        return DateTime.UtcNow - issuedOn <= TimeSpan.FromMinutes(MaxAgeMinutes) ? userId : null;
    }
}
