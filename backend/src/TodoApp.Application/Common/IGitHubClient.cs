namespace TodoApp.Application.Common;

/// <summary>
/// Abstraction over GitHub's standard OAuth Apps flow and REST API,
/// mirroring IJiraClient/IAzureDevOpsClient's shape. GitHub's OAuth is
/// deliberately the simplest of the three: a classic authorization-code
/// exchange with no token expiry/refresh to manage (unlike Jira) and no
/// granular-scope minefield (unlike both Jira and Azure DevOps' now-defunct
/// classic OAuth) — see AzureDevOpsConnection/JiraConnection for that
/// history. The real implementation (GitHubApiClient) lives in
/// Infrastructure.
/// </summary>
public interface IGitHubClient
{
    /// <summary>False when GitHub:ClientId/ClientSecret aren't set — lets callers give a clear "not configured" error instead of building a broken OAuth URL.</summary>
    bool IsConfigured { get; }

    string BuildAuthorizationUrl(string state);

    Task<GitHubTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>The connected account's own login (username) — shown in the UI so a person can confirm which GitHub account they linked, same reasoning as Jira's site name.</summary>
    Task<string?> GetAuthenticatedLoginAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GitHubRepositoryDto>> GetRepositoriesAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GitHubIssueDto>> GetIssuesAsync(string accessToken, string owner, string repo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GitHubCommentDto>> GetCommentsAsync(string accessToken, string owner, string repo, int issueNumber, CancellationToken cancellationToken = default);

    /// <summary>A user's public email by GitHub login, when they've chosen to make it public — otherwise null. See GitHubApiClient for why this needs its own call instead of being included on the issue/comment payload.</summary>
    Task<string?> GetUserEmailAsync(string accessToken, string login, CancellationToken cancellationToken = default);
}

public record GitHubTokenResult(string AccessToken);

public record GitHubRepositoryDto(string Owner, string Name, string FullName);

public record GitHubIssueDto(
    int Number,
    string Title,
    string? Body,
    string State, // GitHub's own "open" | "closed"
    IReadOnlyList<string> Labels,
    string? AssigneeEmail,
    string? AssigneeLogin);

/// <summary>AuthorEmail is very often null — GitHub doesn't expose a user's email through the issues/comments API unless that person has made it public, so this degrades to showing the login only, same as Jira/Azure DevOps' own privacy-setting caveat on comment authors.</summary>
public record GitHubCommentDto(string? AuthorEmail, string AuthorLogin, string Body, DateTime CreatedOn);
