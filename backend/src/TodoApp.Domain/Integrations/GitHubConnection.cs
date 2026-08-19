using TodoApp.Domain.Common;

namespace TodoApp.Domain.Integrations;

/// <summary>
/// One user's link to their GitHub account, via a standard OAuth App (not
/// OAuth 3LO like Jira, not PAT like Azure DevOps) — GitHub's classic OAuth
/// Apps flow issues an access token that, unlike Jira's, doesn't expire by
/// default and needs no refresh token/rotation logic. This is deliberately
/// the simplest of the three integrations' connection models as a result.
/// </summary>
public class GitHubConnection : AggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string AccessTokenEncrypted { get; private set; } = string.Empty;
    public string? GitHubLogin { get; private set; } // the connected GitHub username, shown in the UI so a person can confirm which account they linked
    public DateTime ConnectedOn { get; private set; }

    private GitHubConnection() { }

    private GitHubConnection(string id, string userId, string accessTokenEncrypted, string? gitHubLogin) : base(id)
    {
        UserId = userId;
        AccessTokenEncrypted = accessTokenEncrypted;
        GitHubLogin = gitHubLogin;
        ConnectedOn = DateTime.UtcNow;
    }

    public static GitHubConnection Create(string id, string userId, string accessTokenEncrypted, string? gitHubLogin) =>
        new(id, userId, accessTokenEncrypted, gitHubLogin);

    public static GitHubConnection Rehydrate(string id, string userId, string accessTokenEncrypted, string? gitHubLogin, DateTime connectedOn)
    {
        var connection = new GitHubConnection(id, userId, accessTokenEncrypted, gitHubLogin) { ConnectedOn = connectedOn };
        return connection;
    }

    public void UpdateToken(string accessTokenEncrypted, string? gitHubLogin)
    {
        AccessTokenEncrypted = accessTokenEncrypted;
        GitHubLogin = gitHubLogin;
    }
}
