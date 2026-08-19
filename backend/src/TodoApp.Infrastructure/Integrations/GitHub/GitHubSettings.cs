namespace TodoApp.Infrastructure.Integrations.GitHub;

/// <summary>Config for the GitHub OAuth App registered at github.com/settings/developers.</summary>
public class GitHubSettings
{
    public const string SectionName = "GitHub";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Must exactly match the Authorization callback URL configured on the GitHub OAuth App.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Where the browser is sent back to after the callback finishes (success or failure) — the frontend's public URL.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
