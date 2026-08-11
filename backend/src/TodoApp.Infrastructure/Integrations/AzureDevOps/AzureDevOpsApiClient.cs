using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Integrations.AzureDevOps;

/// <summary>
/// Talks to the Azure DevOps REST API (dev.azure.com), authenticated with a
/// Personal Access Token (PAT) via HTTP Basic auth (empty username, the PAT
/// as the password — Azure DevOps' documented way to use a PAT against its
/// REST API). See AzureDevOpsConnection for why this isn't OAuth: Entra ID
/// apps don't support personal Microsoft accounts for the Azure DevOps
/// resource, and Azure DevOps' own classic OAuth app registration has since
/// been discontinued entirely.
/// </summary>
public class AzureDevOpsApiClient : IAzureDevOpsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureDevOpsApiClient> _logger;

    public AzureDevOpsApiClient(HttpClient httpClient, ILogger<AzureDevOpsApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    private static AuthenticationHeaderValue BasicAuthHeader(string personalAccessToken) =>
        new("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{personalAccessToken}")));

    public async Task<bool> VerifyAccessAsync(string personalAccessToken, string organization, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{Uri.EscapeDataString(organization)}/_apis/projects?api-version=7.0&$top=1");
        request.Headers.Authorization = BasicAuthHeader(personalAccessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Azure DevOps PAT verification failed for org {Organization}: {StatusCode} {Body}", organization, (int)response.StatusCode, body);
        }
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<AzureDevOpsProjectDto>> GetProjectsAsync(string personalAccessToken, string organization, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{Uri.EscapeDataString(organization)}/_apis/projects?api-version=7.0");
        request.Headers.Authorization = BasicAuthHeader(personalAccessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<ProjectPageResponse>(JsonOptions, cancellationToken);
        return (page?.Value ?? new()).Select(p => new AzureDevOpsProjectDto(p.Id, p.Name)).ToList();
    }

    public async Task<IReadOnlyList<AzureDevOpsWorkItemDto>> GetWorkItemsAsync(string personalAccessToken, string organization, string projectName, CancellationToken cancellationToken = default)
    {
        var org = Uri.EscapeDataString(organization);
        var project = Uri.EscapeDataString(projectName);

        using var wiqlRequest = new HttpRequestMessage(HttpMethod.Post, $"https://dev.azure.com/{org}/{project}/_apis/wit/wiql?api-version=7.0")
        {
            Content = JsonContent.Create(new
            {
                query = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{projectName.Replace("'", "''")}' ORDER BY [System.Id] ASC"
            }, options: JsonOptions),
        };
        wiqlRequest.Headers.Authorization = BasicAuthHeader(personalAccessToken);

        using var wiqlResponse = await _httpClient.SendAsync(wiqlRequest, cancellationToken);
        wiqlResponse.EnsureSuccessStatusCode();

        var wiql = await wiqlResponse.Content.ReadFromJsonAsync<WiqlResponse>(JsonOptions, cancellationToken);
        var ids = (wiql?.WorkItems ?? new()).Select(w => w.Id).ToList();
        if (ids.Count == 0) return new List<AzureDevOpsWorkItemDto>();

        var results = new List<AzureDevOpsWorkItemDto>();
        const int batchSize = 200; // Azure DevOps' own cap per batch request
        var fields = "System.Title,System.Description,System.State,Microsoft.VSTS.Common.Priority,System.Tags,System.AssignedTo,Microsoft.VSTS.Scheduling.DueDate,Microsoft.VSTS.Scheduling.StoryPoints";

        for (var offset = 0; offset < ids.Count; offset += batchSize)
        {
            var batchIds = ids.Skip(offset).Take(batchSize).ToList();
            using var batchRequest = new HttpRequestMessage(HttpMethod.Post, $"https://dev.azure.com/{org}/_apis/wit/workitemsbatch?api-version=7.0")
            {
                Content = JsonContent.Create(new { ids = batchIds, fields = fields.Split(',') }, options: JsonOptions),
            };
            batchRequest.Headers.Authorization = BasicAuthHeader(personalAccessToken);

            using var batchResponse = await _httpClient.SendAsync(batchRequest, cancellationToken);
            batchResponse.EnsureSuccessStatusCode();

            var batch = await batchResponse.Content.ReadFromJsonAsync<WorkItemBatchResponse>(JsonOptions, cancellationToken);
            foreach (var item in batch?.Value ?? new())
            {
                var f = item.Fields;
                results.Add(new AzureDevOpsWorkItemDto(
                    item.Id.ToString(),
                    f.Title ?? $"Work item {item.Id}",
                    StripHtml(f.Description),
                    f.State ?? "New",
                    f.Priority?.ToString(),
                    f.DueDate,
                    string.IsNullOrWhiteSpace(f.Tags) ? new List<string>() : f.Tags.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList(),
                    f.AssignedTo?.UniqueName,
                    f.StoryPoints.HasValue ? (int)Math.Round(f.StoryPoints.Value, MidpointRounding.AwayFromZero) : null));
            }
        }

        return results;
    }

    /// <summary>Azure DevOps' description field is HTML, not plain text — a bare tag-strip is good enough for a readable imported description, not meant to preserve formatting.</summary>
    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    // --- Wire DTOs — deliberately kept private to this class; the rest of the app only ever sees IAzureDevOpsClient's own DTOs above. ---

    private record ProjectPageResponse(List<ProjectResponse> Value);

    private record ProjectResponse(string Id, string Name);

    private record WiqlResponse(List<WiqlWorkItemRef> WorkItems);

    private record WiqlWorkItemRef(int Id);

    private record WorkItemBatchResponse(List<WorkItemResponse> Value);

    private record WorkItemResponse(int Id, WorkItemFields Fields);

    private record WorkItemFields(
        [property: JsonPropertyName("System.Title")] string? Title,
        [property: JsonPropertyName("System.Description")] string? Description,
        [property: JsonPropertyName("System.State")] string? State,
        [property: JsonPropertyName("Microsoft.VSTS.Common.Priority")] int? Priority,
        [property: JsonPropertyName("System.Tags")] string? Tags,
        [property: JsonPropertyName("System.AssignedTo")] WorkItemAssignedTo? AssignedTo,
        [property: JsonPropertyName("Microsoft.VSTS.Scheduling.DueDate")] DateTime? DueDate,
        [property: JsonPropertyName("Microsoft.VSTS.Scheduling.StoryPoints")] double? StoryPoints);

    private record WorkItemAssignedTo([property: JsonPropertyName("uniqueName")] string? UniqueName);
}
