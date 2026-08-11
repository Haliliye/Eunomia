namespace TodoApp.Infrastructure.Integrations.AzureDevOps;

/// <summary>Config for the Azure DevOps OAuth app registered at app.vsaex.visualstudio.com/app/register — not an Azure AD/Entra ID app registration, since Entra apps don't support personal Microsoft accounts for the Azure DevOps resource (Microsoft's own stated limitation). See AzureDevOpsApiClient.</summary>
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
