namespace TodoApp.Application.Common;

/// <summary>
/// Abstraction over the Azure DevOps REST API, authenticated via a Personal
/// Access Token (PAT) rather than OAuth — see AzureDevOpsConnection for why.
/// Every method here takes the PAT directly (already decrypted by the
/// caller) rather than an access token, mirroring IJiraClient's shape
/// otherwise. The real implementation (AzureDevOpsApiClient) lives in
/// Infrastructure.
/// </summary>
public interface IAzureDevOpsClient
{
    /// <summary>Verifies a PAT actually works against the given organization before we store it — a wrong/expired/mistyped PAT should fail loudly at connect time, not silently on the next import.</summary>
    Task<bool> VerifyAccessAsync(string personalAccessToken, string organization, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AzureDevOpsProjectDto>> GetProjectsAsync(string personalAccessToken, string organization, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AzureDevOpsWorkItemDto>> GetWorkItemsAsync(string personalAccessToken, string organization, string projectName, CancellationToken cancellationToken = default);

    /// <summary>No batch endpoint exists for comments — one call per work item, same trade-off as Jira's per-issue comment fetch would be if it weren't inline on the issue.</summary>
    Task<IReadOnlyList<AzureDevOpsCommentDto>> GetCommentsAsync(string personalAccessToken, string organization, string projectName, string workItemId, CancellationToken cancellationToken = default);

    Task<Stream> DownloadAttachmentAsync(string personalAccessToken, string downloadUrl, CancellationToken cancellationToken = default);

    /// <summary>The project's iteration (sprint) tree, flattened — see AzureDevOpsIterationDto.</summary>
    Task<IReadOnlyList<AzureDevOpsIterationDto>> GetIterationsAsync(string personalAccessToken, string organization, string projectName, CancellationToken cancellationToken = default);
}

public record AzureDevOpsProjectDto(string Id, string Name);

public record AzureDevOpsWorkItemDto(
    string Id,
    string Title,
    string? Description,
    string StateName,
    string? PriorityName,
    DateTime? DueDate,
    IReadOnlyList<string> Tags,
    string? AssigneeEmail,
    int? StoryPoints,
    IReadOnlyList<AzureDevOpsLinkDto> Links,
    IReadOnlyList<AzureDevOpsAttachmentDto> Attachments,
    string? IterationPath,
    string? ParentWorkItemId);

/// <summary>RelationType is one of Azure DevOps' own link type reference names, trimmed to the part after the last dot for readability (e.g. "Related", "Hierarchy-Forward") — mapped to our StoryLinkType in AzureDevOpsIssueMapper's caller, not here.</summary>
public record AzureDevOpsLinkDto(string TargetWorkItemId, string RelationType);

public record AzureDevOpsAttachmentDto(string FileName, string ContentType, long SizeBytes, string DownloadUrl);

/// <summary>AuthorEmail can be null (same privacy-setting caveat as Jira's AssigneeEmail) — falls back to AuthorDisplayName for the imported comment's byline.</summary>
public record AzureDevOpsCommentDto(string? AuthorEmail, string AuthorDisplayName, string Text, DateTime CreatedOn);

public record AzureDevOpsIterationDto(string Name, DateTime? StartDate, DateTime? FinishDate);
