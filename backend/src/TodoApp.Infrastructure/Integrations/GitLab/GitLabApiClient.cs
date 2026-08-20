using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Integrations.GitLab;

/// <summary>
/// Talks to GitLab's OAuth2 endpoints and REST API v4 (gitlab.com by
/// default — self-managed GitLab instances aren't supported here, only
/// gitlab.com, same simplification GitHub's client makes for github.com
/// specifically over GitHub Enterprise). Unlike GitHubApiClient, token
/// exchange/refresh here follows the same rotating-token shape as
/// JiraApiClient — see IGitLabClient's remarks.
/// </summary>
public class GitLabApiClient : IGitLabClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string ApiBaseUrl = "https://gitlab.com/api/v4";

    private readonly HttpClient _httpClient;
    private readonly GitLabSettings _settings;

    public GitLabApiClient(HttpClient httpClient, IOptions<GitLabSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public bool IsConfigured => _settings.IsConfigured;

    public string BuildAuthorizationUrl(string state)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["redirect_uri"] = _settings.RedirectUri,
            ["response_type"] = "code",
            // "api" is the broadest scope (read/write everything) — GitLab
            // doesn't offer a narrower "issues only" scope the way some
            // APIs do, so this is the minimum that actually works.
            ["scope"] = "api",
            ["state"] = state,
        };
        var queryString = string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        return $"https://gitlab.com/oauth/authorize?{queryString}";
    }

    public Task<GitLabTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default) =>
        RequestTokenAsync(new
        {
            client_id = _settings.ClientId,
            client_secret = _settings.ClientSecret,
            code,
            grant_type = "authorization_code",
            redirect_uri = _settings.RedirectUri,
        }, cancellationToken);

    public Task<GitLabTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        RequestTokenAsync(new
        {
            client_id = _settings.ClientId,
            client_secret = _settings.ClientSecret,
            refresh_token = refreshToken,
            grant_type = "refresh_token",
        }, cancellationToken);

    private async Task<GitLabTokenResult> RequestTokenAsync(object body, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("https://gitlab.com/oauth/token", body, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"GitLab token request failed ({(int)response.StatusCode}): {error}");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("GitLab token response was empty.");

        return new GitLabTokenResult(payload.AccessToken, payload.RefreshToken, DateTime.UtcNow.AddSeconds(payload.ExpiresIn));
    }

    public async Task<string?> GetAuthenticatedUsernameAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"{ApiBaseUrl}/user", accessToken, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var user = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions, cancellationToken);
        return user?.Username;
    }

    public async Task<IReadOnlyList<GitLabProjectDto>> GetProjectsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var results = new List<GitLabProjectDto>();
        // membership=true — projects the user is actually a member of, not
        // every public project on the instance.
        var url = $"{ApiBaseUrl}/projects?membership=true&per_page=100&simple=true";

        while (url is not null)
        {
            using var response = await SendAsync(HttpMethod.Get, url, accessToken, cancellationToken);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>(JsonOptions, cancellationToken) ?? new();
            results.AddRange(page.Select(p => new GitLabProjectDto(p.Id, p.Name, p.PathWithNamespace)));

            url = NextPageUrl(response);
        }

        return results;
    }

    public async Task<IReadOnlyList<GitLabIssueDto>> GetIssuesAsync(string accessToken, int projectId, CancellationToken cancellationToken = default)
    {
        var results = new List<GitLabIssueDto>();
        var url = $"{ApiBaseUrl}/projects/{projectId}/issues?scope=all&per_page=100";

        while (url is not null)
        {
            using var response = await SendAsync(HttpMethod.Get, url, accessToken, cancellationToken);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<List<IssueResponse>>(JsonOptions, cancellationToken) ?? new();
            results.AddRange(page.Select(i => new GitLabIssueDto(
                i.Iid, i.Title, i.Description, i.State, i.Labels,
                AssigneeEmail: null, // see GetUserEmailAsync — resolved separately, GitLab's issue payload never includes it
                i.Assignee?.Username)));

            url = NextPageUrl(response);
        }

        return results;
    }

    public async Task<IReadOnlyList<GitLabNoteDto>> GetNotesAsync(string accessToken, int projectId, int issueIid, CancellationToken cancellationToken = default)
    {
        var results = new List<GitLabNoteDto>();
        var url = $"{ApiBaseUrl}/projects/{projectId}/issues/{issueIid}/notes?per_page=100";

        while (url is not null)
        {
            using var response = await SendAsync(HttpMethod.Get, url, accessToken, cancellationToken);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<List<NoteResponse>>(JsonOptions, cancellationToken) ?? new();
            results.AddRange(page
                // System-generated notes ("changed status to closed", "moved
                // this issue to ...") aren't real comments — GitLab flags
                // these explicitly via "system", unlike GitHub/Jira where
                // the API simply doesn't return them in the first place.
                .Where(n => !n.IsSystemNote)
                .Select(n => new GitLabNoteDto(null, n.Author.Username, n.Body, n.CreatedAt)));

            url = NextPageUrl(response);
        }

        return results;
    }

    public async Task<string?> GetUserEmailAsync(string accessToken, string username, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"{ApiBaseUrl}/users?username={Uri.EscapeDataString(username)}", accessToken, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>(JsonOptions, cancellationToken);
        return users?.FirstOrDefault()?.PublicEmail is { Length: > 0 } email ? email : null;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    /// <summary>GitLab exposes both an X-Next-Page header and an RFC 5988 Link header for pagination — the Link header is used here since it's the same shape GitHubApiClient already parses.</summary>
    private static string? NextPageUrl(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var values)) return null;
        var linkHeader = values.FirstOrDefault();
        if (linkHeader is null) return null;

        foreach (var part in linkHeader.Split(','))
        {
            var segments = part.Split(';');
            if (segments.Length < 2) continue;
            if (!segments[1].Contains("rel=\"next\"")) continue;

            return segments[0].Trim().TrimStart('<').TrimEnd('>');
        }

        return null;
    }

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
    private record UserResponse(string Username, [property: JsonPropertyName("public_email")] string? PublicEmail);
    private record ProjectResponse(int Id, string Name, [property: JsonPropertyName("path_with_namespace")] string PathWithNamespace);
    private record IssueAssignee(string Username);
    private record IssueResponse(int Iid, string Title, string? Description, string State, List<string> Labels, IssueAssignee? Assignee);
    private record NoteAuthor(string Username);
    private record NoteResponse(string Body, NoteAuthor Author, [property: JsonPropertyName("system")] bool IsSystemNote, [property: JsonPropertyName("created_at")] DateTime CreatedAt);
}
