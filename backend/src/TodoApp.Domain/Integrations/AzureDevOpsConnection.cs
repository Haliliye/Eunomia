using TodoApp.Domain.Common;

namespace TodoApp.Domain.Integrations;

/// <summary>
/// One user's link to their Azure DevOps account, established via OAuth 2.0
/// through the Microsoft identity platform (not Azure DevOps' own older
/// app.vssps.visualstudio.com OAuth flow, which requires JWT-signed client
/// assertions for token exchange — the standard Microsoft identity platform
/// flow uses the same authorization-code + client-secret shape as Jira's,
/// so this mirrors JiraConnection closely). Tokens are stored encrypted, see
/// ITokenCipher.
///
/// OrganizationName is set once the user picks which Azure DevOps
/// organization to use (see CompleteAzureDevOpsConnectionCommand) — a
/// Microsoft account can belong to several, unlike Jira's one-site-per-token
/// norm, so this doesn't get filled in until that follow-up step.
/// </summary>
public class AzureDevOpsConnection : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string? OrganizationName { get; private set; }
    public string AccessTokenEncrypted { get; private set; } = string.Empty;
    public string RefreshTokenEncrypted { get; private set; } = string.Empty;
    public DateTime AccessTokenExpiresOn { get; private set; }
    public DateTime ConnectedOn { get; private set; }

    public bool AccessTokenNeedsRefresh => DateTime.UtcNow >= AccessTokenExpiresOn.AddMinutes(-1);

    private AzureDevOpsConnection() { }

    private AzureDevOpsConnection(string id, string userId, string accessTokenEncrypted, string refreshTokenEncrypted, DateTime accessTokenExpiresOn) : base(id)
    {
        UserId = userId;
        AccessTokenEncrypted = accessTokenEncrypted;
        RefreshTokenEncrypted = refreshTokenEncrypted;
        AccessTokenExpiresOn = accessTokenExpiresOn;
        ConnectedOn = DateTime.UtcNow;
    }

    public static AzureDevOpsConnection Create(string id, string userId, string accessTokenEncrypted, string refreshTokenEncrypted, DateTime accessTokenExpiresOn) =>
        new(id, userId, accessTokenEncrypted, refreshTokenEncrypted, accessTokenExpiresOn);

    public static AzureDevOpsConnection Rehydrate(string id, string userId, string? organizationName, string accessTokenEncrypted, string refreshTokenEncrypted, DateTime accessTokenExpiresOn, DateTime connectedOn)
    {
        var connection = new AzureDevOpsConnection(id, userId, accessTokenEncrypted, refreshTokenEncrypted, accessTokenExpiresOn)
        {
            OrganizationName = organizationName,
            ConnectedOn = connectedOn
        };
        return connection;
    }

    public void UpdateTokens(string accessTokenEncrypted, string refreshTokenEncrypted, DateTime accessTokenExpiresOn)
    {
        AccessTokenEncrypted = accessTokenEncrypted;
        RefreshTokenEncrypted = refreshTokenEncrypted;
        AccessTokenExpiresOn = accessTokenExpiresOn;
    }

    public void SetOrganization(string organizationName) => OrganizationName = organizationName;
}
