using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Integrations.Jira;

/// <summary>
/// Talks to Atlassian's identity endpoints (auth.atlassian.com) and the Jira
/// Cloud REST API (api.atlassian.com/ex/jira/{cloudId}/...) via OAuth 2.0
/// (3LO). Uses the classic REST API v2 (not v3) specifically because v3's
/// `description` field is Atlassian Document Format (a nested JSON rich-text
/// tree) — v2 returns it as a plain string, which is all we need and avoids
/// writing an ADF-to-text walker.
/// </summary>
public class JiraApiClient : IJiraClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string AuthBaseUrl = "https://auth.atlassian.com";
    private const string ApiBaseUrl = "https://api.atlassian.com";

    // offline_access is what makes Atlassian issue a refresh token at all —
    // without it, the access token can't be renewed and the user would have
    // to re-authorize every hour.
    //
    // NOTE: during initial testing this app intermittently returned
    // "failed to retrieve client" regardless of scope — the exact same
    // authorize URL succeeded once and failed on retries with identical
    // parameters, pointing to a transient issue on Atlassian's side (likely
    // propagation delay for a freshly created OAuth app) rather than
    // anything wrong with this request. If it recurs, retry rather than
    // assume the scope/params are at fault.
    private const string Scopes = "read:jira-work read:jira-user offline_access";

    private readonly HttpClient _httpClient;
    private readonly JiraSettings _settings;

    public JiraApiClient(HttpClient httpClient, IOptions<JiraSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public string BuildAuthorizationUrl(string state)
    {
        var query = new Dictionary<string, string>
        {
            ["audience"] = "api.atlassian.com",
            ["client_id"] = _settings.ClientId,
            ["scope"] = Scopes,
            ["redirect_uri"] = _settings.RedirectUri,
            ["state"] = state,
            ["response_type"] = "code",
            ["prompt"] = "consent",
        };
        var queryString = string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        return $"{AuthBaseUrl}/authorize?{queryString}";
    }

    public async Task<JiraTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default) =>
        await RequestTokenAsync(new
        {
            grant_type = "authorization_code",
            client_id = _settings.ClientId,
            client_secret = _settings.ClientSecret,
            code,
            redirect_uri = _settings.RedirectUri,
        }, cancellationToken);

    public async Task<JiraTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        await RequestTokenAsync(new
        {
            grant_type = "refresh_token",
            client_id = _settings.ClientId,
            client_secret = _settings.ClientSecret,
            refresh_token = refreshToken,
        }, cancellationToken);

    private async Task<JiraTokenResult> RequestTokenAsync(object body, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"{AuthBaseUrl}/oauth/token", body, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Jira token request failed ({(int)response.StatusCode}): {error}");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Jira token response was empty.");

        return new JiraTokenResult(payload.AccessToken, payload.RefreshToken ?? string.Empty, DateTime.UtcNow.AddSeconds(payload.ExpiresIn));
    }

    public async Task<IReadOnlyList<JiraSiteResource>> GetAccessibleResourcesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/oauth/token/accessible-resources");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var resources = await response.Content.ReadFromJsonAsync<List<AccessibleResource>>(JsonOptions, cancellationToken) ?? new();
        return resources.Select(r => new JiraSiteResource(r.Id, r.Url, r.Name)).ToList();
    }

    public async Task<IReadOnlyList<JiraProjectDto>> GetProjectsAsync(string accessToken, string cloudId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/ex/jira/{cloudId}/rest/api/2/project");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var projects = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>(JsonOptions, cancellationToken) ?? new();
        return projects.Select(p => new JiraProjectDto(p.Key, p.Name, p.AvatarUrls?.FortyEight)).ToList();
    }

    /// <summary>
    /// Story points aren't a fixed field — Jira Cloud stores them as a
    /// per-site custom field (e.g. customfield_10016), whose id varies by
    /// instance and whose display name varies too ("Story Points" on classic
    /// projects, "Story point estimate" on team-managed ones). Discovered
    /// once per import by scanning GET /rest/api/2/field for a name
    /// containing "story point" — null if the site has no such field (e.g.
    /// story points aren't enabled on this project), in which case it's
    /// simply left unmapped, same as any other CSV import that skips it.
    /// </summary>
    private async Task<string?> FindStoryPointsFieldIdAsync(string accessToken, string cloudId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/ex/jira/{cloudId}/rest/api/2/field");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null; // don't fail the whole import over an optional field

        var fields = await response.Content.ReadFromJsonAsync<List<FieldResponse>>(JsonOptions, cancellationToken) ?? new();
        return fields.FirstOrDefault(f => f.Name.Contains("story point", StringComparison.OrdinalIgnoreCase))?.Id;
    }

    public async Task<IReadOnlyList<JiraIssueDto>> GetIssuesAsync(string accessToken, string cloudId, string projectKey, CancellationToken cancellationToken = default)
    {
        var results = new List<JiraIssueDto>();
        string? nextPageToken = null;
        const int pageSize = 100;
        const int maxPages = 20; // safety cap (2000 issues) — the new search/jql endpoint's pagination has been reported flaky upstream; don't loop forever on it

        var storyPointsFieldId = await FindStoryPointsFieldIdAsync(accessToken, cloudId, cancellationToken);
        var requestedFields = storyPointsFieldId is null
            ? new[] { "summary", "description", "status", "priority", "duedate", "labels", "assignee" }
            : new[] { "summary", "description", "status", "priority", "duedate", "labels", "assignee", storyPointsFieldId };

        for (var page = 0; page < maxPages; page++)
        {
            // The old GET/POST /rest/api/2/search was removed by Atlassian
            // (returns 410 Gone as of 2025) — /search/jql is its replacement.
            // Using the v2 path specifically (not v3) keeps `description` as
            // a plain string instead of Atlassian Document Format (ADF), the
            // same reasoning as the rest of this client. Pagination here is
            // nextPageToken-based, not startAt/total — there's no total count
            // in the response at all anymore.
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/ex/jira/{cloudId}/rest/api/2/search/jql")
            {
                Content = JsonContent.Create(new
                {
                    jql = $"project = \"{projectKey}\" ORDER BY created ASC",
                    nextPageToken,
                    maxResults = pageSize,
                    fields = requestedFields,
                }, options: JsonOptions),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var searchPage = await response.Content.ReadFromJsonAsync<SearchResponse>(JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Jira search response was empty.");

            foreach (var issue in searchPage.Issues)
            {
                int? storyPoints = null;
                if (storyPointsFieldId is not null
                    && issue.Fields.ExtraFields.TryGetValue(storyPointsFieldId, out var rawValue)
                    && rawValue.ValueKind is JsonValueKind.Number)
                {
                    // Jira stores this as a decimal (0.5, 1, 2, 3, 5, 8, 13...)
                    // but our domain's StoryPoints is a whole number — round
                    // rather than truncate so e.g. 0.5 doesn't disappear to 0.
                    storyPoints = (int)Math.Round(rawValue.GetDouble(), MidpointRounding.AwayFromZero);
                }

                results.Add(new JiraIssueDto(
                    issue.Key,
                    issue.Fields.Summary,
                    issue.Fields.Description,
                    issue.Fields.Status.Name,
                    issue.Fields.Priority?.Name,
                    DateTime.TryParse(issue.Fields.DueDate, out var due) ? due : null,
                    issue.Fields.Labels ?? new List<string>(),
                    issue.Fields.Assignee?.EmailAddress,
                    storyPoints));
            }

            if (string.IsNullOrEmpty(searchPage.NextPageToken) || searchPage.Issues.Count == 0) break;
            nextPageToken = searchPage.NextPageToken;
        }

        return results;
    }

    // --- Wire DTOs — deliberately kept private to this class; the rest of the app only ever sees IJiraClient's own DTOs above. ---

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private record AccessibleResource(string Id, string Url, string Name);

    private record ProjectResponse(string Key, string Name, [property: JsonPropertyName("avatarUrls")] ProjectAvatarUrls? AvatarUrls);

    private record ProjectAvatarUrls([property: JsonPropertyName("48x48")] string? FortyEight);

    private record SearchResponse(List<IssueResponse> Issues, [property: JsonPropertyName("nextPageToken")] string? NextPageToken);

    private record IssueResponse(string Key, IssueFields Fields);

    private record IssueFields(
        string Summary,
        string? Description,
        IssueStatus Status,
        IssuePriority? Priority,
        [property: JsonPropertyName("duedate")] string? DueDate,
        List<string>? Labels,
        IssueAssignee? Assignee)
    {
        // Catches custom fields we asked for (like story points) but didn't
        // give a strongly-typed property — their key (e.g. "customfield_10016")
        // is only known at runtime, discovered via FindStoryPointsFieldIdAsync.
        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtraFields { get; init; } = new();
    }

    private record IssueStatus(string Name);

    private record IssuePriority(string Name);

    // emailAddress can be null even when an assignee is set — Atlassian's
    // per-user "email visibility" privacy setting can hide it from the API
    // regardless of our scopes. When that happens the story is imported
    // unassigned rather than failing the row (same fallback as a CSV row
    // whose assignee doesn't match any team member).
    private record IssueAssignee([property: JsonPropertyName("emailAddress")] string? EmailAddress);

    private record FieldResponse(string Id, string Name);
}
