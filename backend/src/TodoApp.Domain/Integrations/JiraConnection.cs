using TodoApp.Domain.Common;

namespace TodoApp.Domain.Integrations;

/// <summary>
/// One user's link to their Jira Cloud site, established via OAuth 2.0 (3LO).
/// Tokens are stored encrypted (see ITokenCipher) — never in plain text —
/// because unlike a password hash, these need to be decryptable to actually
/// call the Jira API on the user's behalf.
///
/// Jira Cloud issues rotating refresh tokens: every refresh returns a NEW
/// refresh token that replaces the old one, which is then invalidated. So
/// RefreshTokenEncrypted always holds only the latest one — see
/// UpdateTokens, called after both the initial exchange and every refresh.
///
/// One connection per user (not per team) — a user connects their own Jira
/// account once, then can import from any of their team's backlogs.
/// </summary>
public class JiraConnection : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string CloudId { get; private set; } = string.Empty;
    public string SiteUrl { get; private set; } = string.Empty;
    public string SiteName { get; private set; } = string.Empty;
    public string AccessTokenEncrypted { get; private set; } = string.Empty;
    public string RefreshTokenEncrypted { get; private set; } = string.Empty;
    public DateTime AccessTokenExpiresOn { get; private set; }
    public DateTime ConnectedOn { get; private set; }

    /// <summary>A little slack before the real expiry so a request that starts just before expiry doesn't fail mid-flight.</summary>
    public bool AccessTokenNeedsRefresh => DateTime.UtcNow >= AccessTokenExpiresOn.AddMinutes(-1);

    private JiraConnection() { }

    private JiraConnection(string id, string userId, string cloudId, string siteUrl, string siteName,
        string accessTokenEncrypted, string refreshTokenEncrypted, DateTime accessTokenExpiresOn) : base(id)
    {
        UserId = userId;
        CloudId = cloudId;
        SiteUrl = siteUrl;
        SiteName = siteName;
        AccessTokenEncrypted = accessTokenEncrypted;
        RefreshTokenEncrypted = refreshTokenEncrypted;
        AccessTokenExpiresOn = accessTokenExpiresOn;
        ConnectedOn = DateTime.UtcNow;
    }

    public static JiraConnection Create(string id, string userId, string cloudId, string siteUrl, string siteName,
        string accessTokenEncrypted, string refreshTokenEncrypted, DateTime accessTokenExpiresOn) =>
        new(id, userId, cloudId, siteUrl, siteName, accessTokenEncrypted, refreshTokenEncrypted, accessTokenExpiresOn);

    public static JiraConnection Rehydrate(string id, string userId, string cloudId, string siteUrl, string siteName,
        string accessTokenEncrypted, string refreshTokenEncrypted, DateTime accessTokenExpiresOn, DateTime connectedOn)
    {
        var connection = new JiraConnection(id, userId, cloudId, siteUrl, siteName, accessTokenEncrypted, refreshTokenEncrypted, accessTokenExpiresOn)
        {
            ConnectedOn = connectedOn
        };
        return connection;
    }

    /// <summary>Called after the initial OAuth exchange and after every subsequent refresh (rotating tokens — see class remarks).</summary>
    public void UpdateTokens(string accessTokenEncrypted, string refreshTokenEncrypted, DateTime accessTokenExpiresOn)
    {
        AccessTokenEncrypted = accessTokenEncrypted;
        RefreshTokenEncrypted = refreshTokenEncrypted;
        AccessTokenExpiresOn = accessTokenExpiresOn;
    }
}
