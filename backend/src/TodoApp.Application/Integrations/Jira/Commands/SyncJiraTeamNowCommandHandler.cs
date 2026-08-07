using MediatR;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Integrations;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.Jira.Commands;

public class SyncJiraTeamNowCommandHandler : IRequestHandler<SyncJiraTeamNowCommand, ImportSummaryDto>
{
    private readonly IJiraProjectSyncRepository _syncRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly JiraAccessTokenProvider _accessTokenProvider;
    private readonly JiraProjectImportService _importService;

    public SyncJiraTeamNowCommandHandler(
        IJiraProjectSyncRepository syncRepository,
        ITeamRepository teamRepository,
        JiraAccessTokenProvider accessTokenProvider,
        JiraProjectImportService importService)
    {
        _syncRepository = syncRepository;
        _teamRepository = teamRepository;
        _accessTokenProvider = accessTokenProvider;
        _importService = importService;
    }

    public async Task<ImportSummaryDto> Handle(SyncJiraTeamNowCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var sync = await _syncRepository.GetByTeamIdAsync(request.TeamId, cancellationToken)
            ?? throw new InvalidOperationException("This team isn't linked to a Jira project.");

        // Uses whoever's Jira connection is on file for this link, not
        // necessarily the person clicking "Sync now" — same as the
        // background auto-sync loop, so a manual sync and a scheduled one
        // behave identically.
        var (connection, accessToken) = await _accessTokenProvider.GetValidAccessTokenAsync(sync.ConnectedByUserId, cancellationToken);

        return await _importService.ImportAsync(team, sync.ProjectKey, accessToken, connection.CloudId, sync.ConnectedByUserId, setAutoSync: null, cancellationToken);
    }
}
