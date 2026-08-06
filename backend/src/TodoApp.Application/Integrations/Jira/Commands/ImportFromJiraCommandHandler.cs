using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.Integrations.Jira;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.Jira.Commands;

public class ImportFromJiraCommandHandler : IRequestHandler<ImportFromJiraCommand, ImportSummaryDto>
{
    private readonly JiraAccessTokenProvider _accessTokenProvider;
    private readonly IJiraClient _jiraClient;
    private readonly ITeamRepository _teamRepository;
    private readonly JiraProjectImportService _importService;

    public ImportFromJiraCommandHandler(
        JiraAccessTokenProvider accessTokenProvider,
        IJiraClient jiraClient,
        ITeamRepository teamRepository,
        JiraProjectImportService importService)
    {
        _accessTokenProvider = accessTokenProvider;
        _jiraClient = jiraClient;
        _teamRepository = teamRepository;
        _importService = importService;
    }

    public async Task<ImportSummaryDto> Handle(ImportFromJiraCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        // Same permission level as the CSV import and sprint management — a
        // bulk-creation action, not open to every member.
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var (connection, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(request.RequestingUserId, cancellationToken);
        var issues = await _jiraClient.GetIssuesAsync(accessToken, connection.CloudId, request.ProjectKey, cancellationToken);

        return await _importService.ImportAsync(team, issues, request.RequestingUserId, cancellationToken);
    }
}
