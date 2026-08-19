using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Integrations.GitHub;

/// <summary>
/// Talks to GitHub's OAuth Apps endpoints (github.com) and REST API
/// (api.github.com). Two things GitHub's API insists on that other REST
/// APIs used in this project don't: a User-Agent header on every request
/// (requests without one are rejected outright), and an explicit
/// "Accept: application/json" header on the token exchange specifically —
/// without it, GitHub replies with a form-urlencoded body instead of JSON.
/// </summary>
public class GitHubApiClient : IGitHubClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string UserAgent = "Eunomia-App";

    private readonly HttpClient _httpClient;
    private readonly GitHubSettings _settings;
    private readonly ILogger<GitHubApiClient> _logger;

    public GitHubApiClient(HttpClient httpClient, IOptions<GitHubSettings> settings, ILogger<GitHubApiClient> logger)
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
            ["redirect_uri"] = _settings.RedirectUri,
            // "repo" covers both public and private repo issue access; a
            // narrower "public_repo" would silently exclude private repos
            // someone might reasonably want to import from.
            ["scope"] = "repo",
            ["state"] = state,
        };
        var queryString = string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        return $"https://github.com/login/oauth/authorize?{queryString}";
    }

    public async Task<GitHubTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = JsonContent.Create(new
        {
            client_id = _settings.ClientId,
            client_secret = _settings.ClientSecret,
            code,
            redirect_uri = _settings.RedirectUri,
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken);
        if (string.IsNullOrWhiteSpace(token?.AccessToken))
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("GitHub token exchange returned no access_token: {Body}", body);
            throw new InvalidOperationException("GitHub did not return an access token. The authorization code may have expired or already been used.");
        }

        return new GitHubTokenResult(token.AccessToken);
    }

    public async Task<string?> GetAuthenticatedLoginAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "https://api.github.com/user", accessToken, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var user = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions, cancellationToken);
        return user?.Login;
    }

    public async Task<IReadOnlyList<GitHubRepositoryDto>> GetRepositoriesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var results = new List<GitHubRepositoryDto>();
        // affiliation covers repos the person owns, collaborates on, and
        // has access to via an org — the broadest reasonable "everything
        // you could plausibly import from" set.
        var url = "https://api.github.com/user/repos?per_page=100&affiliation=owner,collaborator,organization_member";

        while (url is not null)
        {
            using var response = await SendAsync(HttpMethod.Get, url, accessToken, cancellationToken);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<List<RepoResponse>>(JsonOptions, cancellationToken) ?? new();
            results.AddRange(page.Select(r => new GitHubRepositoryDto(r.Owner.Login, r.Name, r.FullName)));

            url = NextPageUrl(response);
        }

        return results;
    }

    public async Task<IReadOnlyList<GitHubIssueDto>> GetIssuesAsync(string accessToken, string owner, string repo, CancellationToken cancellationToken = default)
    {
        var results = new List<GitHubIssueDto>();
        var url = $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues?state=all&per_page=100";

        while (url is not null)
        {
            using var response = await SendAsync(HttpMethod.Get, url, accessToken, cancellationToken);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<List<IssueResponse>>(JsonOptions, cancellationToken) ?? new();
            foreach (var issue in page)
            {
                // GitHub's issues endpoint returns pull requests too — a PR
                // is "an issue with extra fields", distinguished only by the
                // presence of this one property. Importing PRs as stories
                // would be noise, not a bug worth surfacing to the user.
                if (issue.PullRequest is not null) continue;

                results.Add(new GitHubIssueDto(
                    issue.Number,
                    issue.Title,
                    issue.Body,
                    issue.State,
                    issue.Labels.Select(l => l.Name).ToList(),
                    AssigneeEmail: null, // see GetUserEmailAsync — resolved separately, GitHub's issue payload never includes it
                    issue.Assignee?.Login));
            }

            url = NextPageUrl(response);
        }

        return results;
    }

    public async Task<IReadOnlyList<GitHubCommentDto>> GetCommentsAsync(string accessToken, string owner, string repo, int issueNumber, CancellationToken cancellationToken = default)
    {
        var results = new List<GitHubCommentDto>();
        var url = $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues/{issueNumber}/comments?per_page=100";

        while (url is not null)
        {
            using var response = await SendAsync(HttpMethod.Get, url, accessToken, cancellationToken);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<List<CommentResponse>>(JsonOptions, cancellationToken) ?? new();
            results.AddRange(page.Select(c => new GitHubCommentDto(null, c.User.Login, c.Body, c.CreatedAt)));

            url = NextPageUrl(response);
        }

        return results;
    }

    /// <summary>
    /// A user's email, looked up only when actually needed (per-assignee,
    /// not per-issue) since it costs an extra API call. Very often returns
    /// null: GitHub only exposes it here if the account owner has set their
    /// email to public in their profile settings — same graceful-degradation
    /// caveat as Jira/Azure DevOps' own assignee-matching limits.
    /// </summary>
    public async Task<string?> GetUserEmailAsync(string accessToken, string login, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"https://api.github.com/users/{Uri.EscapeDataString(login)}", accessToken, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var user = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions, cancellationToken);
        return user?.Email;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    /// <summary>GitHub paginates via an RFC 5988 Link header (rel="next"), not a page-number/offset query param — this parses that header instead of guessing a next URL.</summary>
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

            var url = segments[0].Trim().TrimStart('<').TrimEnd('>');
            return url;
        }

        return null;
    }

    private record TokenResponse([property: JsonPropertyName("access_token")] string? AccessToken);
    private record UserResponse(string Login, string? Email);
    private record RepoOwner(string Login);
    private record RepoResponse(string Name, [property: JsonPropertyName("full_name")] string FullName, RepoOwner Owner);
    private record IssueLabel(string Name);
    private record IssueAssignee(string Login);
    private record PullRequestMarker(); // presence-only — its fields are never read, just whether the property is null
    private record IssueResponse(int Number, string Title, string? Body, string State, List<IssueLabel> Labels, IssueAssignee? Assignee, [property: JsonPropertyName("pull_request")] PullRequestMarker? PullRequest);
    private record CommentUser(string Login);
    private record CommentResponse(string Body, CommentUser User, [property: JsonPropertyName("created_at")] DateTime CreatedAt);
}
