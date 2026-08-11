namespace TodoApp.Application.Common;

/// <summary>
/// Abstraction over Azure DevOps' OAuth (Microsoft identity platform) + REST
/// API, mirroring IJiraClient's shape — Application/handlers don't depend on
/// HttpClient or Microsoft/Azure DevOps' specific endpoints directly. The
/// real implementation (AzureDevOpsApiClient) lives in Infrastructure.
/// </summary>
public interface IAzureDevOpsClient
{
    string BuildAuthorizationUrl(string state);

    Task<AzureDevOpsTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);

    Task<AzureDevOpsTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Every Azure DevOps organization this account can access — a Microsoft account, unlike a Jira token, can span several unrelated orgs, so the person picks one after connecting.</summary>
    Task<IReadOnlyList<string>> GetOrganizationsAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AzureDevOpsProjectDto>> GetProjectsAsync(string accessToken, string organization, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AzureDevOpsWorkItemDto>> GetWorkItemsAsync(string accessToken, string organization, string projectName, CancellationToken cancellationToken = default);
}

public record AzureDevOpsTokenResult(string AccessToken, string RefreshToken, DateTime ExpiresOn);

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
