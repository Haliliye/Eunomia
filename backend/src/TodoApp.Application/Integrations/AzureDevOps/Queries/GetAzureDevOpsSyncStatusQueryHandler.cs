using MediatR;
using TodoApp.Application.Integrations;
using TodoApp.Domain.Integrations;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.AzureDevOps.Queries;

public class GetAzureDevOpsSyncStatusQueryHandler : IRequestHandler<GetAzureDevOpsSyncStatusQuery, AzureDevOpsSyncStatusDto>
{
    private readonly IAzureDevOpsProjectSyncRepository _syncRepository;
    private readonly ITeamRepository _teamRepository;

    public GetAzureDevOpsSyncStatusQueryHandler(IAzureDevOpsProjectSyncRepository syncRepository, ITeamRepository teamRepository)
    {
        _syncRepository = syncRepository;
        _teamRepository = teamRepository;
    }

    public async Task<AzureDevOpsSyncStatusDto> Handle(GetAzureDevOpsSyncStatusQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var sync = await _syncRepository.GetByTeamIdAsync(request.TeamId, cancellationToken);
        return sync is null
            ? new AzureDevOpsSyncStatusDto(false, null, false, null)
            : new AzureDevOpsSyncStatusDto(true, sync.ProjectName, sync.AutoSyncEnabled, sync.LastSyncedOn,
                sync.History.Select(h => new SyncLogEntryDto(h.SyncedOn, h.CreatedCount, h.UpdatedCount, h.SkippedCount)).ToList());
    }
}
