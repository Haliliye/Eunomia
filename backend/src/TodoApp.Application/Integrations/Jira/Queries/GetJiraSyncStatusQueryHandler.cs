using MediatR;
using TodoApp.Application.Integrations;
using TodoApp.Domain.Integrations;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.Jira.Queries;

public class GetJiraSyncStatusQueryHandler : IRequestHandler<GetJiraSyncStatusQuery, JiraSyncStatusDto>
{
    private readonly IJiraProjectSyncRepository _syncRepository;
    private readonly ITeamRepository _teamRepository;

    public GetJiraSyncStatusQueryHandler(IJiraProjectSyncRepository syncRepository, ITeamRepository teamRepository)
    {
        _syncRepository = syncRepository;
        _teamRepository = teamRepository;
    }

    public async Task<JiraSyncStatusDto> Handle(GetJiraSyncStatusQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsMember(request.RequestingUserId);

        var sync = await _syncRepository.GetByTeamIdAsync(request.TeamId, cancellationToken);
        return sync is null
            ? new JiraSyncStatusDto(false, null, false, null)
            : new JiraSyncStatusDto(true, sync.ProjectKey, sync.AutoSyncEnabled, sync.LastSyncedOn,
                sync.History.Select(h => new SyncLogEntryDto(h.SyncedOn, h.CreatedCount, h.UpdatedCount, h.SkippedCount)).ToList());
    }
}
