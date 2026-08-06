using MediatR;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.Jira.Commands;

public class ImportFromJiraCommandHandler : IRequestHandler<ImportFromJiraCommand, ImportSummaryDto>
{
    private readonly JiraAccessTokenProvider _accessTokenProvider;
    private readonly ITeamRepository _teamRepository;
    private readonly JiraProjectImportService _importService;

    public ImportFromJiraCommandHandler(
        JiraAccessTokenProvider accessTokenProvider,
        ITeamRepository teamRepository,
        JiraProjectImportService importService)
    {
        _accessTokenProvider = accessTokenProvider;
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

        return await _importService.ImportAsync(team, request.ProjectKey, accessToken, connection.CloudId, request.RequestingUserId, request.SetAutoSync, cancellationToken);
    }
}
