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

    /// <summary>Downloads an attachment's raw bytes — url comes from JiraAttachmentDto.DownloadUrl, which is only ever a URL Jira itself gave us in an issue response (never user-supplied), so no separate host allow-list is needed here.</summary>
    Task<Stream> DownloadAttachmentAsync(string accessToken, string downloadUrl, CancellationToken cancellationToken = default);

    /// <summary>Every sprint (any state) on the Scrum board(s) attached to this project — see JiraApiClient for why a board lookup has to happen first. Empty for Kanban-only or team-managed projects with no Scrum board.</summary>
    Task<IReadOnlyList<JiraSprintDto>> GetSprintsAsync(string accessToken, string cloudId, string projectKey, CancellationToken cancellationToken = default);
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
    IReadOnlyList<string> Labels,
    string? AssigneeEmail,
    int? StoryPoints,
    IReadOnlyList<JiraIssueLinkDto> Links,
    IReadOnlyList<JiraCommentDto> Comments,
    IReadOnlyList<JiraAttachmentDto> Attachments,
    string? SprintName);

/// <summary>One side of a Jira issue link — TargetIssueKey is the *other* issue, LinkTypeRaw is Jira's own phrase for the relationship (e.g. "blocks", "is blocked by", "relates to"), mapped to our StoryLinkType in JiraIssueMapper since Jira's vocabulary is effectively unbounded (apps can add custom link types).</summary>
public record JiraIssueLinkDto(string TargetIssueKey, string LinkTypeRaw);

/// <summary>AuthorEmail follows the same "may be null due to Jira's privacy setting" rule as JiraIssueDto.AssigneeEmail — falls back to AuthorDisplayName for the imported comment's byline when it's missing.</summary>
public record JiraCommentDto(string? AuthorEmail, string AuthorDisplayName, string BodyText, DateTime CreatedOn);

public record JiraAttachmentDto(string FileName, string ContentType, long SizeBytes, string DownloadUrl);

public record JiraSprintDto(string Name, DateTime? StartDate, DateTime? EndDate, string State);
