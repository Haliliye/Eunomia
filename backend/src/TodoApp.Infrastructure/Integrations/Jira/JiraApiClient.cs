using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Integrations.Jira;

/// <summary>
/// Talks to Atlassian's identity endpoints (auth.atlassian.com), the Jira
/// Cloud REST API (api.atlassian.com/ex/jira/{cloudId}/rest/api/2/...), and
/// the Jira Agile REST API (.../rest/agile/1.0/...) for sprints, via OAuth
/// 2.0 (3LO). Uses the classic REST API v2 (not v3) specifically because
/// v3's `description` field is Atlassian Document Format (a nested JSON
/// rich-text tree) — v2 returns it as a plain string, which is all we need
/// and avoids writing an ADF-to-text walker. Comment bodies come back as
/// ADF even on v2 (the comment API predates the plain-text option), so
/// those get a minimal ADF-to-text walk — see ExtractPlainText.
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
    /// Story points and Sprint aren't fixed fields — Jira Cloud stores both
    /// as per-site custom fields (e.g. customfield_10016), whose id varies
    /// by instance and whose display name varies too ("Story Points" on
    /// classic projects, "Story point estimate" on team-managed ones).
    /// Discovered once per import by scanning GET /rest/api/2/field for a
    /// name containing the given text — null if the site has no such field,
    /// in which case that piece is simply left unmapped, same as any other
    /// CSV import that skips an optional column.
    /// </summary>
    private async Task<string?> FindFieldIdAsync(string accessToken, string cloudId, string nameContains, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/ex/jira/{cloudId}/rest/api/2/field");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null; // don't fail the whole import over an optional field

        var fields = await response.Content.ReadFromJsonAsync<List<FieldResponse>>(JsonOptions, cancellationToken) ?? new();
        return fields.FirstOrDefault(f => f.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    public async Task<IReadOnlyList<JiraIssueDto>> GetIssuesAsync(string accessToken, string cloudId, string projectKey, CancellationToken cancellationToken = default)
    {
        var results = new List<JiraIssueDto>();
        string? nextPageToken = null;
        const int pageSize = 100;
        const int maxPages = 20; // safety cap (2000 issues) — the new search/jql endpoint's pagination has been reported flaky upstream; don't loop forever on it

        var storyPointsFieldId = await FindFieldIdAsync(accessToken, cloudId, "story point", cancellationToken);
        var sprintFieldId = await FindFieldIdAsync(accessToken, cloudId, "sprint", cancellationToken);

        var requestedFields = new List<string> { "summary", "description", "status", "priority", "duedate", "labels", "assignee", "issuelinks", "comment", "attachment" };
        if (storyPointsFieldId is not null) requestedFields.Add(storyPointsFieldId);
        if (sprintFieldId is not null) requestedFields.Add(sprintFieldId);

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
                    && issue.Fields.ExtraFields.TryGetValue(storyPointsFieldId, out var rawStoryPoints)
                    && rawStoryPoints.ValueKind is JsonValueKind.Number)
                {
                    // Jira stores this as a decimal (0.5, 1, 2, 3, 5, 8, 13...)
                    // but our domain's StoryPoints is a whole number — round
                    // rather than truncate so e.g. 0.5 doesn't disappear to 0.
                    storyPoints = (int)Math.Round(rawStoryPoints.GetDouble(), MidpointRounding.AwayFromZero);
                }

                string? sprintName = null;
                if (sprintFieldId is not null
                    && issue.Fields.ExtraFields.TryGetValue(sprintFieldId, out var rawSprint)
                    && rawSprint.ValueKind is JsonValueKind.Array)
                {
                    // An issue can carry a history of sprints it's moved
                    // through (backlog -> Sprint 1 -> Sprint 2, all present in
                    // this array) — the last entry is always the current one.
                    var last = rawSprint.EnumerateArray().LastOrDefault();
                    if (last.ValueKind == JsonValueKind.Object && last.TryGetProperty("name", out var nameEl))
                        sprintName = nameEl.GetString();
                }

                var links = (issue.Fields.IssueLinks ?? new())
                    .Select(l => l.OutwardIssue is not null
                        ? new JiraIssueLinkDto(l.OutwardIssue.Key, l.Type.Outward)
                        : l.InwardIssue is not null
                            ? new JiraIssueLinkDto(l.InwardIssue.Key, l.Type.Inward)
                            : null)
                    .Where(l => l is not null)
                    .Cast<JiraIssueLinkDto>()
                    .ToList();

                var comments = (issue.Fields.Comment?.Comments ?? new())
                    .Select(c => new JiraCommentDto(
                        c.Author?.EmailAddress,
                        c.Author?.DisplayName ?? "Unknown",
                        ExtractPlainText(c.Body),
                        DateTime.TryParse(c.Created, out var created) ? created : DateTime.UtcNow))
                    .ToList();

                var attachments = (issue.Fields.Attachment ?? new())
                    .Select(a => new JiraAttachmentDto(a.Filename, a.MimeType, a.Size, a.Content))
                    .ToList();

                results.Add(new JiraIssueDto(
                    issue.Key,
                    issue.Fields.Summary,
                    issue.Fields.Description,
                    issue.Fields.Status.Name,
                    issue.Fields.Priority?.Name,
                    DateTime.TryParse(issue.Fields.DueDate, out var due) ? due : null,
                    issue.Fields.Labels ?? new List<string>(),
                    issue.Fields.Assignee?.EmailAddress,
                    storyPoints,
                    links,
                    comments,
                    attachments,
                    sprintName));
            }

            if (string.IsNullOrEmpty(searchPage.NextPageToken) || searchPage.Issues.Count == 0) break;
            nextPageToken = searchPage.NextPageToken;
        }

        return results;
    }

    /// <summary>
    /// Minimal Atlassian Document Format walker — comment bodies come back
    /// as ADF's nested JSON tree (unlike `description`, which v2 gives us as
    /// plain text). Only pulls out "text" nodes and joins paragraphs with
    /// blank lines; deliberately doesn't reconstruct formatting (bold,
    /// lists, mentions, etc.) — good enough for an imported comment's
    /// content to be readable, not pixel-identical to Jira's rendering.
    /// </summary>
    private static string ExtractPlainText(JsonElement? adfNode)
    {
        if (adfNode is null || adfNode.Value.ValueKind != JsonValueKind.Object) return string.Empty;

        var parts = new List<string>();
        void Walk(JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.Object)
            {
                if (node.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "text"
                    && node.TryGetProperty("text", out var textEl))
                    parts.Add(textEl.GetString() ?? string.Empty);

                if (node.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.Array)
                    foreach (var child in contentEl.EnumerateArray())
                        Walk(child);

                if (node.TryGetProperty("type", out var blockTypeEl) && blockTypeEl.GetString() is "paragraph" or "heading")
                    parts.Add("\n");
            }
        }
        Walk(adfNode.Value);

        return string.Join(" ", parts).Replace(" \n ", "\n\n").Trim();
    }

    public async Task<Stream> DownloadAttachmentAsync(string accessToken, string downloadUrl, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JiraSprintDto>> GetSprintsAsync(string accessToken, string cloudId, string projectKey, CancellationToken cancellationToken = default)
    {
        // Sprints live on a *board*, not directly on a project — a project
        // can have zero (Kanban-only), one, or multiple boards. We fetch
        // every board's sprints and dedupe by name, since almost every real
        // project has exactly one Scrum board.
        using var boardRequest = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/ex/jira/{cloudId}/rest/agile/1.0/board?projectKeyOrId={Uri.EscapeDataString(projectKey)}");
        boardRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var boardResponse = await _httpClient.SendAsync(boardRequest, cancellationToken);
        if (!boardResponse.IsSuccessStatusCode) return new List<JiraSprintDto>(); // no Agile access / no boards — not fatal, sprints are optional

        var boardPage = await boardResponse.Content.ReadFromJsonAsync<BoardPageResponse>(JsonOptions, cancellationToken);
        var scrumBoards = (boardPage?.Values ?? new()).Where(b => b.Type == "scrum").ToList();

        var sprintsByName = new Dictionary<string, JiraSprintDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var board in scrumBoards)
        {
            using var sprintRequest = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/ex/jira/{cloudId}/rest/agile/1.0/board/{board.Id}/sprint?maxResults=50");
            sprintRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var sprintResponse = await _httpClient.SendAsync(sprintRequest, cancellationToken);
            if (!sprintResponse.IsSuccessStatusCode) continue;

            var sprintPage = await sprintResponse.Content.ReadFromJsonAsync<SprintPageResponse>(JsonOptions, cancellationToken);
            foreach (var sprint in sprintPage?.Values ?? new())
                sprintsByName[sprint.Name] = new JiraSprintDto(sprint.Name, sprint.StartDate, sprint.EndDate, sprint.State);
        }

        return sprintsByName.Values.ToList();
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
        IssueAssignee? Assignee,
        [property: JsonPropertyName("issuelinks")] List<IssueLink>? IssueLinks,
        IssueCommentField? Comment,
        List<IssueAttachment>? Attachment)
    {
        // Catches custom fields we asked for (like story points/sprint) but
        // didn't give a strongly-typed property — their key (e.g.
        // "customfield_10016") is only known at runtime, discovered via
        // FindFieldIdAsync.
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

    private record IssueLink(IssueLinkType Type, [property: JsonPropertyName("outwardIssue")] IssueLinkTarget? OutwardIssue, [property: JsonPropertyName("inwardIssue")] IssueLinkTarget? InwardIssue);

    private record IssueLinkType(string Outward, string Inward);

    private record IssueLinkTarget(string Key);

    private record IssueCommentField(List<IssueComment> Comments);

    private record IssueComment(IssueCommentAuthor? Author, JsonElement Body, string? Created);

    private record IssueCommentAuthor([property: JsonPropertyName("emailAddress")] string? EmailAddress, [property: JsonPropertyName("displayName")] string? DisplayName);

    private record IssueAttachment(string Filename, [property: JsonPropertyName("mimeType")] string MimeType, long Size, string Content);

    private record BoardPageResponse(List<BoardResponse> Values);

    private record BoardResponse(long Id, string Name, string Type);

    private record SprintPageResponse(List<SprintResponse> Values);

    private record SprintResponse(string Name, string State, DateTime? StartDate, DateTime? EndDate);
}
