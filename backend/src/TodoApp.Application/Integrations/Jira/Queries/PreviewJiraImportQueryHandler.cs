using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Integrations.Jira;
using TodoApp.Application.UserStories.Commands.ImportUserStories;

namespace TodoApp.Application.Integrations.Jira.Queries;

public class PreviewJiraImportQueryHandler : IRequestHandler<PreviewJiraImportQuery, IReadOnlyList<ImportRowDto>>
{
    private readonly JiraAccessTokenProvider _accessTokenProvider;
    private readonly IJiraClient _jiraClient;

    public PreviewJiraImportQueryHandler(JiraAccessTokenProvider accessTokenProvider, IJiraClient jiraClient)
    {
        _accessTokenProvider = accessTokenProvider;
        _jiraClient = jiraClient;
    }

    public async Task<IReadOnlyList<ImportRowDto>> Handle(PreviewJiraImportQuery request, CancellationToken cancellationToken)
    {
        var (connection, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(request.RequestingUserId, cancellationToken);
        var issues = await _jiraClient.GetIssuesAsync(accessToken, connection.CloudId, request.ProjectKey, cancellationToken);

        // Preview doesn't touch the database (no team column creation here —
        // that only happens on the real import, see
        // JiraProjectImportService.EnsureColumnsForStatuses), so it just
        // shows each issue's actual Jira status name as-is rather than
        // pre-resolving it to a column key.
        var identityStatusMap = issues.Select(i => i.StatusName).Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(s => s, s => s, StringComparer.OrdinalIgnoreCase);

        return JiraIssueMapper.MapAndValidate(issues, identityStatusMap);
    }
}
