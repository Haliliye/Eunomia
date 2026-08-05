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
        return JiraIssueMapper.MapAndValidate(issues);
    }
}
