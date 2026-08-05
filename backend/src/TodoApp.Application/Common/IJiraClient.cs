namespace TodoApp.Application.Common;

/// <summary>
/// Abstraction over Jira Cloud's OAuth 2.0 (3LO) + REST API so
/// Application/handlers don't depend on HttpClient or Atlassian's specific
/// endpoints directly — the real implementation (JiraApiClient) lives in
/// Infrastructure. All token values passed in/out are the raw (decrypted)
/// values; encryption at rest is the caller's responsibility (see
/// ITokenCipher), not this client's.
/// </summary>
public interface IJiraClient
{
    /// <summary>Builds the full authorize.atlassian.com URL the user's browser is redirected to. State is an opaque, caller-generated anti-CSRF value round-tripped back to the callback.</summary>
    string BuildAuthorizationUrl(string state);

    /// <summary>Exchanges the one-time authorization code (from the callback) for an access + refresh token pair.</summary>
    Task<JiraTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Uses a (rotating) refresh token to obtain a new access + refresh token pair. The old refresh token is invalidated by Atlassian the moment this succeeds.</summary>
    Task<JiraTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>The Jira sites (cloudId + URL) this access token can reach — normally just one for a typical account.</summary>
    Task<IReadOnlyList<JiraSiteResource>> GetAccessibleResourcesAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JiraProjectDto>> GetProjectsAsync(string accessToken, string cloudId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JiraIssueDto>> GetIssuesAsync(string accessToken, string cloudId, string projectKey, CancellationToken cancellationToken = default);
}

public record JiraTokenResult(string AccessToken, string RefreshToken, DateTime ExpiresOn);

public record JiraSiteResource(string CloudId, string Url, string Name);

public record JiraProjectDto(string Key, string Name, string? AvatarUrl);

public record JiraIssueDto(
    string Key,
    string Summary,
    string? Description,
    string StatusName,
    string? PriorityName,
    DateTime? DueDate,
    IReadOnlyList<string> Labels);
