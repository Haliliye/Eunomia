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
    int? StoryPoints);
