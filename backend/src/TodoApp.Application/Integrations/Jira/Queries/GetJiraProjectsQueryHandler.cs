using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Integrations.Jira;

namespace TodoApp.Application.Integrations.Jira.Queries;

public class GetJiraProjectsQueryHandler : IRequestHandler<GetJiraProjectsQuery, IReadOnlyList<JiraProjectDto>>
{
    private readonly JiraAccessTokenProvider _accessTokenProvider;
    private readonly IJiraClient _jiraClient;

    public GetJiraProjectsQueryHandler(JiraAccessTokenProvider accessTokenProvider, IJiraClient jiraClient)
    {
        _accessTokenProvider = accessTokenProvider;
        _jiraClient = jiraClient;
    }

    public async Task<IReadOnlyList<JiraProjectDto>> Handle(GetJiraProjectsQuery request, CancellationToken cancellationToken)
    {
        var (connection, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(request.RequestingUserId, cancellationToken);
        return await _jiraClient.GetProjectsAsync(accessToken, connection.CloudId, cancellationToken);
    }
}
