namespace TodoApp.Application.Common;

/// <summary>
/// Abstraction over GitLab's standard OAuth2 application flow and REST API
/// (v4), mirroring IJiraClient's shape — unlike IGitHubClient, this needs a
/// RefreshTokenAsync because GitLab's OAuth tokens expire (2 hours by
/// default, unlike GitHub's classic OAuth Apps). The real implementation
/// (GitLabApiClient) lives in Infrastructure.
/// </summary>
public interface IGitLabClient
{
    /// <summary>False when GitLab:ClientId/ClientSecret aren't set — lets callers give a clear "not configured" error instead of building a broken OAuth URL.</summary>
    bool IsConfigured { get; }

    string BuildAuthorizationUrl(string state);

    Task<GitLabTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);

    Task<GitLabTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>The connected account's own username — shown in the UI so a person can confirm which GitLab account they linked.</summary>
    Task<string?> GetAuthenticatedUsernameAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GitLabProjectDto>> GetProjectsAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GitLabIssueDto>> GetIssuesAsync(string accessToken, int projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GitLabNoteDto>> GetNotesAsync(string accessToken, int projectId, int issueIid, CancellationToken cancellationToken = default);

    /// <summary>A user's public email by GitLab username, when they've set one — otherwise null. Same reasoning as IGitHubClient.GetUserEmailAsync: GitLab's issue/note payloads never include an assignee/author's email directly.</summary>
    Task<string?> GetUserEmailAsync(string accessToken, string username, CancellationToken cancellationToken = default);
}

public record GitLabTokenResult(string AccessToken, string RefreshToken, DateTime ExpiresOn);

public record GitLabProjectDto(int Id, string Name, string PathWithNamespace);

public record GitLabIssueDto(
    int Iid, // project-scoped issue number shown in GitLab's UI ("#7") — Id is a separate, instance-wide identifier GitLab's UI never shows
    string Title,
    string? Description,
    string State, // GitLab's own "opened" | "closed"
    IReadOnlyList<string> Labels,
    string? AssigneeEmail,
    string? AssigneeUsername);

/// <summary>AuthorEmail is very often null — same privacy caveat as GitHubCommentDto.</summary>
public record GitLabNoteDto(string? AuthorEmail, string AuthorUsername, string Body, DateTime CreatedOn);
