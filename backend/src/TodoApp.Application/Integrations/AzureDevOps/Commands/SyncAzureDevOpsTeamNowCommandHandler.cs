using MediatR;
using TodoApp.Application.Integrations.AzureDevOps;
using TodoApp.Application.Common;
using TodoApp.Application.UserStories.Commands.ImportUserStories;
using TodoApp.Domain.Integrations;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public class SyncAzureDevOpsTeamNowCommandHandler : IRequestHandler<SyncAzureDevOpsTeamNowCommand, ImportSummaryDto>
{
    private readonly IAzureDevOpsProjectSyncRepository _syncRepository;
    private readonly IAzureDevOpsConnectionRepository _connectionRepository;
    private readonly ITokenCipher _tokenCipher;
    private readonly ITeamRepository _teamRepository;
    private readonly AzureDevOpsProjectImportService _importService;

    public SyncAzureDevOpsTeamNowCommandHandler(
        IAzureDevOpsProjectSyncRepository syncRepository,
        IAzureDevOpsConnectionRepository connectionRepository,
        ITokenCipher tokenCipher,
        ITeamRepository teamRepository,
        AzureDevOpsProjectImportService importService)
    {
        _syncRepository = syncRepository;
        _connectionRepository = connectionRepository;
        _tokenCipher = tokenCipher;
        _teamRepository = teamRepository;
        _importService = importService;
    }

    public async Task<ImportSummaryDto> Handle(SyncAzureDevOpsTeamNowCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var sync = await _syncRepository.GetByTeamIdAsync(request.TeamId, cancellationToken)
            ?? throw new InvalidOperationException("This team isn't linked to an Azure DevOps project.");

        var connection = await _connectionRepository.GetByUserIdAsync(sync.ConnectedByUserId, cancellationToken)
            ?? throw new InvalidOperationException("The Azure DevOps connection for this sync no longer exists.");
        var pat = _tokenCipher.Decrypt(connection.PersonalAccessTokenEncrypted);

        return await _importService.ImportAsync(team, connection.OrganizationName, sync.ProjectName, pat, sync.ConnectedByUserId, setAutoSync: null, cancellationToken);
    }
}
