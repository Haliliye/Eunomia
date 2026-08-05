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

    // NOTE: offline_access is deliberately NOT requested — including it makes
    // Atlassian reject the authorize request outright with a generic
    // "failed to retrieve client" error for this app (root cause not fully
    // identified; confirmed reproducible by isolating this scope alone).
    // Consequence: no refresh token is issued, so the access token (~1hr
    // lifetime) can't be silently renewed — JiraAccessTokenProvider surfaces
    // a clear "reconnect" error once it expires instead of a confusing
    // refresh failure.
    private const string Scopes = "read:jira-work read:jira-user";

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

    public async Task<IReadOnlyList<JiraIssueDto>> GetIssuesAsync(string accessToken, string cloudId, string projectKey, CancellationToken cancellationToken = default)
    {
        var results = new List<JiraIssueDto>();
        var startAt = 0;
        const int pageSize = 100;

        while (true)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/ex/jira/{cloudId}/rest/api/2/search")
            {
                Content = JsonContent.Create(new
                {
                    jql = $"project = \"{projectKey}\" ORDER BY created ASC",
                    startAt,
                    maxResults = pageSize,
                    fields = new[] { "summary", "description", "status", "priority", "duedate", "labels" },
                }, options: JsonOptions),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<SearchResponse>(JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Jira search response was empty.");

            foreach (var issue in page.Issues)
            {
                results.Add(new JiraIssueDto(
                    issue.Key,
                    issue.Fields.Summary,
                    issue.Fields.Description,
                    issue.Fields.Status.Name,
                    issue.Fields.Priority?.Name,
                    DateTime.TryParse(issue.Fields.DueDate, out var due) ? due : null,
                    issue.Fields.Labels ?? new List<string>()));
            }

            startAt += page.Issues.Count;
            if (startAt >= page.Total || page.Issues.Count == 0) break;
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

    private record SearchResponse(int Total, List<IssueResponse> Issues);

    private record IssueResponse(string Key, IssueFields Fields);

    private record IssueFields(
        string Summary,
        string? Description,
        IssueStatus Status,
        IssuePriority? Priority,
        [property: JsonPropertyName("duedate")] string? DueDate,
        List<string>? Labels);

    private record IssueStatus(string Name);

    private record IssuePriority(string Name);
}
