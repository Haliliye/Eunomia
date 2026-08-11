using TodoApp.Application.Common;
using TodoApp.Application.Integrations.AzureDevOps;
using TodoApp.Domain.Integrations;
using TodoApp.Domain.Teams;

namespace TodoApp.Api.BackgroundServices;

/// <summary>
/// Mirrors JiraAutoSyncBackgroundService — periodically re-imports every
/// team with AutoSyncEnabled on its AzureDevOpsProjectSync record. No
/// "is Azure DevOps configured" guard is needed the way Jira's has one:
/// the PAT-based client has no client id/secret to be unconfigured, it
/// just does nothing if there are no enabled sync records.
/// </summary>
public class AzureDevOpsAutoSyncBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AzureDevOpsAutoSyncBackgroundService> _logger;

    public AzureDevOpsAutoSyncBackgroundService(IServiceScopeFactory scopeFactory, ILogger<AzureDevOpsAutoSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure DevOps auto-sync cycle failed.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task SyncAllAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var syncRepository = scope.ServiceProvider.GetRequiredService<IAzureDevOpsProjectSyncRepository>();
        var connectionRepository = scope.ServiceProvider.GetRequiredService<IAzureDevOpsConnectionRepository>();
        var tokenCipher = scope.ServiceProvider.GetRequiredService<ITokenCipher>();
        var teamRepository = scope.ServiceProvider.GetRequiredService<ITeamRepository>();
        var importService = scope.ServiceProvider.GetRequiredService<AzureDevOpsProjectImportService>();

        var syncs = await syncRepository.GetAllAutoSyncEnabledAsync(cancellationToken);

        foreach (var sync in syncs)
        {
            try
            {
                var team = await teamRepository.GetByIdAsync(sync.TeamId, cancellationToken);
                if (team is null) continue;

                var connection = await connectionRepository.GetByUserIdAsync(sync.ConnectedByUserId, cancellationToken);
                if (connection is null) continue; // the person disconnected Azure DevOps since — nothing to sync with until they reconnect

                var pat = tokenCipher.Decrypt(connection.PersonalAccessTokenEncrypted);
                await importService.ImportAsync(team, connection.OrganizationName, sync.ProjectName, pat, sync.ConnectedByUserId, setAutoSync: null, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure DevOps auto-sync failed for team {TeamId} (project {ProjectName})", sync.TeamId, sync.ProjectName);
            }
        }
    }
}
