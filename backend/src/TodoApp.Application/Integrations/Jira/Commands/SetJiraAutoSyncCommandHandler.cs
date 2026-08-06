using MediatR;
using TodoApp.Domain.Integrations;
using TodoApp.Domain.Teams;

namespace TodoApp.Application.Integrations.Jira.Commands;

public class SetJiraAutoSyncCommandHandler : IRequestHandler<SetJiraAutoSyncCommand>
{
    private readonly IJiraProjectSyncRepository _syncRepository;
    private readonly ITeamRepository _teamRepository;

    public SetJiraAutoSyncCommandHandler(IJiraProjectSyncRepository syncRepository, ITeamRepository teamRepository)
    {
        _syncRepository = syncRepository;
        _teamRepository = teamRepository;
    }

    public async Task Handle(SetJiraAutoSyncCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");
        team.EnsureIsOwnerOrAdmin(request.RequestingUserId);

        var sync = await _syncRepository.GetByTeamIdAsync(request.TeamId, cancellationToken)
            ?? throw new InvalidOperationException("This team wasn't imported from Jira, so there's nothing to sync.");

        sync.SetAutoSync(request.Enabled, request.RequestingUserId);
        await _syncRepository.UpdateAsync(sync, cancellationToken);
    }
}
