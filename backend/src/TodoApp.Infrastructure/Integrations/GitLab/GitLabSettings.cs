namespace TodoApp.Infrastructure.Integrations.GitLab;

/// <summary>Config for the GitLab OAuth application registered at gitlab.com/-/profile/applications.</summary>
public class GitLabSettings
{
    public const string SectionName = "GitLab";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Must exactly match the Redirect URI configured on the GitLab application.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Where the browser is sent back to after the callback finishes (success or failure) — the frontend's public URL.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
