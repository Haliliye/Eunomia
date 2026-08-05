namespace TodoApp.Infrastructure.Integrations.Jira;

/// <summary>Config for the Atlassian OAuth 2.0 (3LO) app registered at developer.atlassian.com/console — see /areas/yeni-proje-csharp-cqrs.md for the setup steps already completed.</summary>
public class JiraSettings
{
    public const string SectionName = "Jira";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Must exactly match one of the Callback URLs configured in the Atlassian app.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Where the browser is sent back to after the callback finishes (success or failure) — the frontend's public URL.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
