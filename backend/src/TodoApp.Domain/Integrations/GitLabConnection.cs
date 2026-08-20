using TodoApp.Domain.Common;

namespace TodoApp.Domain.Integrations;

/// <summary>
/// One user's link to their GitLab account, via GitLab's standard OAuth2
/// application flow. Unlike GitHub's classic OAuth Apps (GitHubConnection),
/// GitLab's OAuth tokens DO expire (2 hours by default) and issue a
/// refresh token — same rotating-refresh-token shape as JiraConnection, so
/// this mirrors that class rather than GitHubConnection's simpler one.
/// </summary>
public class GitLabConnection : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string? GitLabUsername { get; private set; }
    public string AccessTokenEncrypted { get; private set; } = string.Empty;
    public string RefreshTokenEncrypted { get; private set; } = string.Empty;
    public DateTime AccessTokenExpiresOn { get; private set; }
    public DateTime ConnectedOn { get; private set; }

    /// <summary>A little slack before the real expiry so a request that starts just before expiry doesn't fail mid-flight — same reasoning as JiraConnection.AccessTokenNeedsRefresh.</summary>
    public bool AccessTokenNeedsRefresh => DateTime.UtcNow >= AccessTokenExpiresOn.AddMinutes(-1);

    private GitLabConnection() { }

    private GitLabConnection(string id, string userId, string? gitLabUsername, string accessTokenEncrypted, string refreshTokenEncrypted, DateTime accessTokenExpiresOn) : base(id)
    {
        UserId = userId;
        GitLabUsername = gitLabUsername;
        AccessTokenEncrypted = accessTokenEncrypted;
        RefreshTokenEncrypted = refreshTokenEncrypted;
        AccessTokenExpiresOn = accessTokenExpiresOn;
        ConnectedOn = DateTime.UtcNow;
    }

    public static GitLabConnection Create(string id, string userId, string? gitLabUsername, string accessTokenEncrypted, string refreshTokenEncrypted, DateTime accessTokenExpiresOn) =>
        new(id, userId, gitLabUsername, accessTokenEncrypted, refreshTokenEncrypted, accessTokenExpiresOn);

    public static GitLabConnection Rehydrate(string id, string userId, string? gitLabUsername, string accessTokenEncrypted, string refreshTokenEncrypted, DateTime accessTokenExpiresOn, DateTime connectedOn)
    {
        var connection = new GitLabConnection(id, userId, gitLabUsername, accessTokenEncrypted, refreshTokenEncrypted, accessTokenExpiresOn) { ConnectedOn = connectedOn };
        return connection;
    }

    /// <summary>Called after the initial OAuth exchange and after every subsequent refresh.</summary>
    public void UpdateTokens(string accessTokenEncrypted, string refreshTokenEncrypted, DateTime accessTokenExpiresOn, string? gitLabUsername)
    {
        AccessTokenEncrypted = accessTokenEncrypted;
        RefreshTokenEncrypted = refreshTokenEncrypted;
        AccessTokenExpiresOn = accessTokenExpiresOn;
        if (gitLabUsername is not null) GitLabUsername = gitLabUsername;
    }
}
