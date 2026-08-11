using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Integrations.AzureDevOps;

/// <summary>
/// Talks to the Microsoft identity platform (login.microsoftonline.com) for
/// OAuth and the Azure DevOps REST API (dev.azure.com / app.vssps.visualstudio.com)
/// for everything else. Uses the standard Microsoft identity platform
/// authorization-code flow (not Azure DevOps' older app.vssps.visualstudio.com
/// OAuth, which needs a JWT-signed client assertion for token exchange) —
/// same request/response shape as Jira's OAuth, see JiraApiClient.
/// </summary>
public class AzureDevOpsApiClient : IAzureDevOpsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // "organizations" (not "common") — Azure DevOps requires a work/school
    // or Microsoft account that's a member of at least one org; using the
    // narrower tenant endpoint avoids surfacing personal-only accounts that
    // could never have an org anyway.
    private const string AuthBaseUrl = "https://login.microsoftonline.com/organizations/oauth2/v2.0";

    // Azure DevOps' fixed resource Application ID — every Azure DevOps OAuth
    // integration requests scopes under this id, it's not specific to our app.
    private const string AzureDevOpsResourceId = "499b84ac-1321-427f-aa17-267ca6975798";
    private const string Scopes = $"{AzureDevOpsResourceId}/user_impersonation offline_access";

    private readonly HttpClient _httpClient;
    private readonly AzureDevOpsSettings _settings;
    private readonly ILogger<AzureDevOpsApiClient> _logger;

    public AzureDevOpsApiClient(HttpClient httpClient, IOptions<AzureDevOpsSettings> settings, ILogger<AzureDevOpsApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public string BuildAuthorizationUrl(string state)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _settings.RedirectUri,
            ["response_mode"] = "query",
            ["scope"] = Scopes,
            ["state"] = state,
        };
        var queryString = string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        return $"{AuthBaseUrl}/authorize?{queryString}";
    }

    public async Task<AzureDevOpsTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default) =>
        await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = _settings.RedirectUri,
            ["scope"] = Scopes,
        }, cancellationToken);

    public async Task<AzureDevOpsTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["refresh_token"] = refreshToken,
            ["scope"] = Scopes,
        }, cancellationToken);

    private async Task<AzureDevOpsTokenResult> RequestTokenAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(form);
        using var response = await _httpClient.PostAsync($"{AuthBaseUrl}/token", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Azure DevOps token request failed ({(int)response.StatusCode}): {error}");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Azure DevOps token response was empty.");

        return new AzureDevOpsTokenResult(payload.AccessToken, payload.RefreshToken ?? string.Empty, DateTime.UtcNow.AddSeconds(payload.ExpiresIn));
    }

    public async Task<IReadOnlyList<string>> GetOrganizationsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "https://app.vssps.visualstudio.com/_apis/profile/profiles/me?api-version=7.0");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var meResponse = await _httpClient.SendAsync(meRequest, cancellationToken);
        if (!meResponse.IsSuccessStatusCode)
        {
            var body = await meResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Azure DevOps profile lookup failed: {StatusCode} {Body}", (int)meResponse.StatusCode, body);
            return new List<string>();
        }

        var me = await meResponse.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions, cancellationToken);
        if (me?.Id is null) return new List<string>();

        using var accountsRequest = new HttpRequestMessage(HttpMethod.Get, $"https://app.vssps.visualstudio.com/_apis/accounts?memberId={me.Id}&api-version=7.0");
        accountsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var accountsResponse = await _httpClient.SendAsync(accountsRequest, cancellationToken);
        if (!accountsResponse.IsSuccessStatusCode)
        {
            var body = await accountsResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Azure DevOps accounts lookup failed: {StatusCode} {Body}", (int)accountsResponse.StatusCode, body);
            return new List<string>();
        }

        var accounts = await accountsResponse.Content.ReadFromJsonAsync<AccountsResponse>(JsonOptions, cancellationToken);
        return (accounts?.Value ?? new()).Select(a => a.AccountName).ToList();
    }

    public async Task<IReadOnlyList<AzureDevOpsProjectDto>> GetProjectsAsync(string accessToken, string organization, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{Uri.EscapeDataString(organization)}/_apis/projects?api-version=7.0");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<ProjectPageResponse>(JsonOptions, cancellationToken);
        return (page?.Value ?? new()).Select(p => new AzureDevOpsProjectDto(p.Id, p.Name)).ToList();
    }

    public async Task<IReadOnlyList<AzureDevOpsWorkItemDto>> GetWorkItemsAsync(string accessToken, string organization, string projectName, CancellationToken cancellationToken = default)
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
        wiqlRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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
            batchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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

    /// <summary>Azure DevOps' description field is HTML, not plain text (unlike Jira's v2 API) — a bare tag-strip is good enough for a readable imported description, not meant to preserve formatting.</summary>
    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    // --- Wire DTOs — deliberately kept private to this class; the rest of the app only ever sees IAzureDevOpsClient's own DTOs above. ---

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private record ProfileResponse(string? Id);

    private record AccountsResponse(List<AccountResponse> Value);

    private record AccountResponse([property: JsonPropertyName("accountName")] string AccountName);

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
