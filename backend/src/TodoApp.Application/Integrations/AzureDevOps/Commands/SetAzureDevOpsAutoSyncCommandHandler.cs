using MediatR;
using TodoApp.Domain.Integrations;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.AzureDevOps.Commands;

public class SetAzureDevOpsAutoSyncCommandHandler : IRequestHandler<SetAzureDevOpsAutoSyncCommand>
{
    private readonly IAzureDevOpsProjectSyncRepository _syncRepository;
    private readonly ITeamRepository _teamRepository;

    public SetAzureDevOpsAutoSyncCommandHandler(IAzureDevOpsProjectSyncRepository syncRepository, ITeamRepository teamRepository)
    {
        _syncRepository = syncRepository;
        _teamRepository = teamRepository;
    }

    public async Task Handle(SetAzureDevOpsAutoSyncCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var sync = await _syncRepository.GetByTeamIdAsync(request.TeamId, cancellationToken)
            ?? throw new InvalidOperationException("This team wasn't imported from Azure DevOps, so there's nothing to sync.");

        sync.SetAutoSync(request.Enabled, request.RequestingUserId);
        await _syncRepository.UpdateAsync(sync, cancellationToken);
    }
}
