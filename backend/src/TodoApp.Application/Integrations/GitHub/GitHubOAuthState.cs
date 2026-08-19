namespace TodoApp.Application.Integrations.GitHub;

/// <summary>Packs/unpacks the plaintext payload inside the encrypted OAuth "state" parameter — same reasoning and format as JiraOAuthState, kept as its own copy rather than a shared helper so each integration's OAuth state format can evolve independently.</summary>
internal static class GitHubOAuthState
{
    private const int MaxAgeMinutes = 10;

    public static string Protect(string userId) => $"{userId}|{DateTime.UtcNow.Ticks}";

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
