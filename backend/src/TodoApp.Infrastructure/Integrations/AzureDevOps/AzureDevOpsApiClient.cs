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
/// REST API). See AzureDevOpsConnection for why this isn't OAuth.
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

        for (var offset = 0; offset < ids.Count; offset += batchSize)
        {
            var batchIds = ids.Skip(offset).Take(batchSize).ToList();
            // $expand=relations returns every relation (links, hierarchy,
            // attachments) alongside the full field set — the "fields"
            // filter list is mutually exclusive with $expand on this
            // endpoint, so this fetches more data per item than a plain
            // field-filtered batch would, but it's the only way to get
            // links/attachments/hierarchy without a second round trip.
            using var batchRequest = new HttpRequestMessage(HttpMethod.Post, $"https://dev.azure.com/{org}/_apis/wit/workitemsbatch?api-version=7.0")
            {
                Content = JsonContent.Create(new { ids = batchIds, expand = "relations" }, options: JsonOptions),
            };
            batchRequest.Headers.Authorization = BasicAuthHeader(personalAccessToken);

            using var batchResponse = await _httpClient.SendAsync(batchRequest, cancellationToken);
            batchResponse.EnsureSuccessStatusCode();

            var batch = await batchResponse.Content.ReadFromJsonAsync<WorkItemBatchResponse>(JsonOptions, cancellationToken);
            foreach (var item in batch?.Value ?? new())
            {
                var f = item.Fields;

                var links = new List<AzureDevOpsLinkDto>();
                var attachments = new List<AzureDevOpsAttachmentDto>();
                string? parentWorkItemId = null;

                foreach (var relation in item.Relations ?? new())
                {
                    if (relation.Rel == "AttachedFile")
                    {
                        var fileName = relation.Attributes?.Name ?? "attachment";
                        attachments.Add(new AzureDevOpsAttachmentDto(fileName, "application/octet-stream", relation.Attributes?.ResourceSize ?? 0, relation.Url));
                        continue;
                    }

                    if (!relation.Rel.StartsWith("System.LinkTypes.", StringComparison.Ordinal)) continue; // skip hyperlinks/other non-work-item relations

                    var targetId = ExtractWorkItemIdFromUrl(relation.Url);
                    if (targetId is null) continue;

                    if (relation.Rel == "System.LinkTypes.Hierarchy-Reverse")
                    {
                        parentWorkItemId = targetId; // this work item's own parent
                        continue;
                    }
                    if (relation.Rel == "System.LinkTypes.Hierarchy-Forward") continue; // a child of this item — the child's own Hierarchy-Reverse relation covers this from its side

                    var relationType = relation.Rel[(relation.Rel.LastIndexOf('.') + 1)..];
                    links.Add(new AzureDevOpsLinkDto(targetId, relationType));
                }

                int? storyPoints = null;
                if (f.StoryPoints.HasValue)
                    storyPoints = (int)Math.Round(f.StoryPoints.Value, MidpointRounding.AwayFromZero);

                results.Add(new AzureDevOpsWorkItemDto(
                    item.Id.ToString(),
                    f.Title ?? $"Work item {item.Id}",
                    StripHtml(f.Description),
                    f.State ?? "New",
                    f.Priority?.ToString(),
                    f.DueDate,
                    string.IsNullOrWhiteSpace(f.Tags) ? new List<string>() : f.Tags.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList(),
                    f.AssignedTo?.UniqueName,
                    storyPoints,
                    links,
                    attachments,
                    f.IterationPath,
                    parentWorkItemId));
            }
        }

        return results;
    }

    /// <summary>Relation URLs look like ".../_apis/wit/workItems/1234" — the numeric id is the last path segment.</summary>
    private static string? ExtractWorkItemIdFromUrl(string url)
    {
        var lastSegment = url.TrimEnd('/').Split('/').LastOrDefault();
        return lastSegment is not null && int.TryParse(lastSegment, out _) ? lastSegment : null;
    }

    public async Task<IReadOnlyList<AzureDevOpsCommentDto>> GetCommentsAsync(string personalAccessToken, string organization, string projectName, string workItemId, CancellationToken cancellationToken = default)
    {
        var org = Uri.EscapeDataString(organization);
        var project = Uri.EscapeDataString(projectName);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{org}/{project}/_apis/wit/workItems/{workItemId}/comments?api-version=7.0-preview.3");
        request.Headers.Authorization = BasicAuthHeader(personalAccessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return new List<AzureDevOpsCommentDto>(); // comments are a nice-to-have — don't fail the import over one item's comment fetch

        var page = await response.Content.ReadFromJsonAsync<CommentPageResponse>(JsonOptions, cancellationToken);
        return (page?.Comments ?? new())
            .Select(c => new AzureDevOpsCommentDto(
                c.CreatedBy?.UniqueName,
                c.CreatedBy?.DisplayName ?? "Unknown",
                StripHtml(c.Text) ?? string.Empty,
                c.CreatedDate ?? DateTime.UtcNow))
            .ToList();
    }

    public async Task<Stream> DownloadAttachmentAsync(string personalAccessToken, string downloadUrl, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        request.Headers.Authorization = BasicAuthHeader(personalAccessToken);

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AzureDevOpsIterationDto>> GetIterationsAsync(string personalAccessToken, string organization, string projectName, CancellationToken cancellationToken = default)
    {
        var org = Uri.EscapeDataString(organization);
        var project = Uri.EscapeDataString(projectName);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{org}/{project}/_apis/wit/classificationnodes/iterations?$depth=10&api-version=7.0");
        request.Headers.Authorization = BasicAuthHeader(personalAccessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Azure DevOps iterations lookup failed for project {Project}: {StatusCode} {Body}", projectName, (int)response.StatusCode, body);
            return new List<AzureDevOpsIterationDto>();
        }

        var root = await response.Content.ReadFromJsonAsync<IterationNode>(JsonOptions, cancellationToken);
        var flattened = new List<AzureDevOpsIterationDto>();
        FlattenIterations(root, flattened);
        return flattened;
    }

    /// <summary>Iterations form a tree (e.g. "Release 1 > Sprint 1 > Sprint 1.1") — flattened here since our Sprint domain has no such hierarchy; matched to work items by leaf name only (see AzureDevOpsProjectImportService), same simplification Jira's sprint import makes.</summary>
    private static void FlattenIterations(IterationNode? node, List<AzureDevOpsIterationDto> results)
    {
        if (node is null) return;

        // The synthetic root node (the project itself) never has dates —
        // only real iteration nodes underneath it do.
        if (node.Attributes?.StartDate is not null || node.Attributes?.FinishDate is not null)
            results.Add(new AzureDevOpsIterationDto(node.Name, node.Attributes?.StartDate, node.Attributes?.FinishDate));

        foreach (var child in node.Children ?? new())
            FlattenIterations(child, results);
    }

    /// <summary>Azure DevOps' description/comment text fields are HTML, not plain text — a bare tag-strip is good enough for readable imported content, not meant to preserve formatting.</summary>
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

    private record WorkItemResponse(int Id, WorkItemFields Fields, List<WorkItemRelation>? Relations);

    private record WorkItemFields(
        [property: JsonPropertyName("System.Title")] string? Title,
        [property: JsonPropertyName("System.Description")] string? Description,
        [property: JsonPropertyName("System.State")] string? State,
        [property: JsonPropertyName("Microsoft.VSTS.Common.Priority")] int? Priority,
        [property: JsonPropertyName("System.Tags")] string? Tags,
        [property: JsonPropertyName("System.AssignedTo")] WorkItemAssignedTo? AssignedTo,
        [property: JsonPropertyName("Microsoft.VSTS.Scheduling.DueDate")] DateTime? DueDate,
        [property: JsonPropertyName("Microsoft.VSTS.Scheduling.StoryPoints")] double? StoryPoints,
        [property: JsonPropertyName("System.IterationPath")] string? IterationPath);

    private record WorkItemAssignedTo([property: JsonPropertyName("uniqueName")] string? UniqueName);

    private record WorkItemRelation(string Rel, string Url, WorkItemRelationAttributes? Attributes);

    private record WorkItemRelationAttributes(string? Name, [property: JsonPropertyName("resourceSize")] long? ResourceSize);

    private record CommentPageResponse(List<CommentResponse> Comments);

    private record CommentResponse(CommentAuthor? CreatedBy, string? Text, DateTime? CreatedDate);

    private record CommentAuthor([property: JsonPropertyName("uniqueName")] string? UniqueName, [property: JsonPropertyName("displayName")] string? DisplayName);

    private record IterationNode(string Name, IterationAttributes? Attributes, List<IterationNode>? Children);

    private record IterationAttributes(DateTime? StartDate, DateTime? FinishDate);
}
