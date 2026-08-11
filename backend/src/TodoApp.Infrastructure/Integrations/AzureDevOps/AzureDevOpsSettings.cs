namespace TodoApp.Infrastructure.Integrations.AzureDevOps;

/// <summary>Config for the Azure AD (Microsoft identity platform) app registration used for Azure DevOps OAuth — see /areas setup notes.</summary>
public class AzureDevOpsSettings
{
    public const string SectionName = "AzureDevOps";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Must exactly match a redirect URI configured on the Azure AD app registration.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Where the browser is sent back to after the callback finishes (success or failure) — the frontend's public URL.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
